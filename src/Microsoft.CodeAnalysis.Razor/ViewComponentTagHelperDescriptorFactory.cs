﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Evolution;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class ViewComponentTagHelperDescriptorFactory
    {
        private readonly INamedTypeSymbol _viewComponentAttributeSymbol;
        private readonly INamedTypeSymbol _genericTaskSymbol;
        private readonly INamedTypeSymbol _taskSymbol;
        private readonly INamedTypeSymbol _iDictionarySymbol;

        private static readonly SymbolDisplayFormat FullNameTypeDisplayFormat =
            SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
                .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions & (~SymbolDisplayMiscellaneousOptions.UseSpecialTypes));

        public ViewComponentTagHelperDescriptorFactory(Compilation compilation)
        {
            _viewComponentAttributeSymbol = compilation.GetTypeByMetadataName(ViewComponentTypes.ViewComponentAttribute);
            _genericTaskSymbol = compilation.GetTypeByMetadataName(ViewComponentTypes.GenericTask);
            _taskSymbol = compilation.GetTypeByMetadataName(ViewComponentTypes.Task);
            _iDictionarySymbol = compilation.GetTypeByMetadataName(TagHelperTypes.IDictionary);
        }

        public virtual TagHelperDescriptor CreateDescriptor(INamedTypeSymbol type)
        {
            var assemblyName = type.ContainingAssembly.Name;
            var shortName = GetShortName(type);
            var tagName = $"vc:{DefaultTagHelperDescriptorFactory.ToHtmlCase(shortName)}";
            var typeName = $"__Generated__{shortName}ViewComponentTagHelper";

            var descriptor = new TagHelperDescriptor
            {
                TagName = tagName,
                TypeName = typeName,
                AssemblyName = assemblyName
            };

            SetAttributeDescriptors(type, descriptor);

            descriptor.PropertyBag.Add(ViewComponentTypes.ViewComponentNameKey, shortName);

            return descriptor;
        }

        private void SetAttributeDescriptors(INamedTypeSymbol type, TagHelperDescriptor descriptor)
        {
            var methodParameters = GetInvokeMethodParameters(type);
            var attributeDescriptors = new List<TagHelperAttributeDescriptor>();
            var indexerDescriptors = new List<TagHelperAttributeDescriptor>();
            var requiredAttributeDescriptors = new List<TagHelperRequiredAttributeDescriptor>();

            foreach (var parameter in methodParameters)
            {
                var lowerKebabName = DefaultTagHelperDescriptorFactory.ToHtmlCase(parameter.Name);
                var typeName = parameter.Type.ToDisplayString(FullNameTypeDisplayFormat);
                var attributeDescriptor = new TagHelperAttributeDescriptor
                {
                    Name = lowerKebabName,
                    PropertyName = parameter.Name,
                    TypeName = typeName
                };

                attributeDescriptor.IsEnum = parameter.Type.TypeKind == TypeKind.Enum;
                attributeDescriptor.IsIndexer = false;

                attributeDescriptors.Add(attributeDescriptor);

                var indexerDescriptor = GetIndexerAttributeDescriptor(parameter, lowerKebabName);
                if (indexerDescriptor != null)
                {
                    indexerDescriptors.Add(indexerDescriptor);
                }
                else
                {
                    // Set required attributes only for non-indexer attributes. Indexer attributes can't be required attributes
                    // because there are two ways of setting values for the attribute.
                    requiredAttributeDescriptors.Add(new TagHelperRequiredAttributeDescriptor
                    {
                        Name = lowerKebabName
                    });
                }
            }

            attributeDescriptors.AddRange(indexerDescriptors);
            descriptor.Attributes = attributeDescriptors;
            descriptor.RequiredAttributes = requiredAttributeDescriptors;
        }

        private TagHelperAttributeDescriptor GetIndexerAttributeDescriptor(IParameterSymbol parameter, string name)
        {
            INamedTypeSymbol dictionaryType;
            if ((parameter.Type as INamedTypeSymbol)?.ConstructedFrom == _iDictionarySymbol)
            {
                dictionaryType = (INamedTypeSymbol)parameter.Type;
            }
            else if (parameter.Type.AllInterfaces.Any(s => s.ConstructedFrom == _iDictionarySymbol))
            {
                dictionaryType = parameter.Type.AllInterfaces.First(s => s.ConstructedFrom == _iDictionarySymbol);
            }
            else
            {
                dictionaryType = null;
            }

            if (dictionaryType == null || dictionaryType.TypeArguments[0].SpecialType != SpecialType.System_String)
            {
                return null;
            }

            var type = dictionaryType.TypeArguments[1];
            var descriptor = new TagHelperAttributeDescriptor
            {
                Name = name + "-",
                PropertyName = parameter.Name,
                TypeName = type.ToDisplayString(FullNameTypeDisplayFormat),
                IsEnum = type.TypeKind == TypeKind.Enum,
                IsIndexer = true
            };

            return descriptor;
        }

        private ImmutableArray<IParameterSymbol> GetInvokeMethodParameters(INamedTypeSymbol componentType)
        {
            var methods = componentType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(method =>
                    method.DeclaredAccessibility == Accessibility.Public &&
                    (string.Equals(method.Name, ViewComponentTypes.AsyncMethodName, StringComparison.Ordinal) ||
                    string.Equals(method.Name, ViewComponentTypes.SyncMethodName, StringComparison.Ordinal)))
                .ToArray();

            if (methods.Length == 0)
            {
                throw new InvalidOperationException(
                    ViewComponentResources.FormatViewComponent_CannotFindMethod(ViewComponentTypes.SyncMethodName, ViewComponentTypes.AsyncMethodName, componentType.ToDisplayString(FullNameTypeDisplayFormat)));
            }
            else if (methods.Length > 1)
            {
                throw new InvalidOperationException(
                    ViewComponentResources.FormatViewComponent_AmbiguousMethods(componentType.ToDisplayString(FullNameTypeDisplayFormat), ViewComponentTypes.AsyncMethodName, ViewComponentTypes.SyncMethodName));
            }

            var selectedMethod = methods[0];
            var returnType = selectedMethod.ReturnType as INamedTypeSymbol;
            if (string.Equals(selectedMethod.Name, ViewComponentTypes.AsyncMethodName, StringComparison.Ordinal) && returnType != null)
            {
                if (!returnType.IsGenericType == true ||
                    returnType.ConstructedFrom == _genericTaskSymbol)
                {
                    throw new InvalidOperationException(ViewComponentResources.FormatViewComponent_AsyncMethod_ShouldReturnTask(
                        ViewComponentTypes.AsyncMethodName,
                        componentType.ToDisplayString(FullNameTypeDisplayFormat),
                        nameof(Task)));
                }
            }
            else if (returnType != null)
            {
                // Will invoke synchronously. Method must not return void, Task or Task<T>.
                if (returnType.SpecialType == SpecialType.System_Void)
                {
                    throw new InvalidOperationException(ViewComponentResources.FormatViewComponent_SyncMethod_ShouldReturnValue(
                        ViewComponentTypes.SyncMethodName,
                        componentType.ToDisplayString(FullNameTypeDisplayFormat)));
                }

                var inheritsFromTask = false;
                var currentType = returnType;
                while (currentType != null)
                {
                    if (currentType == _taskSymbol)
                    {
                        inheritsFromTask = true;
                        break;
                    }

                    currentType = currentType.BaseType;
                }

                if (inheritsFromTask)
                {
                    throw new InvalidOperationException(ViewComponentResources.FormatViewComponent_SyncMethod_CannotReturnTask(
                        ViewComponentTypes.SyncMethodName,
                        componentType.ToDisplayString(FullNameTypeDisplayFormat),
                        nameof(Task)));
                }
            }

            var methodParameters = selectedMethod.Parameters;

            return methodParameters;
        }

        private string GetShortName(INamedTypeSymbol componentType)
        {
            var viewComponentAttribute = componentType.GetAttributes().Where(a => a.AttributeClass == _viewComponentAttributeSymbol).FirstOrDefault();
            var name = viewComponentAttribute
                ?.NamedArguments
                .Where(namedArgument => string.Equals(namedArgument.Key, ViewComponentTypes.ViewComponent.Name, StringComparison.Ordinal))
                .FirstOrDefault()
                .Value
                .Value as string;

            if (!string.IsNullOrEmpty(name))
            {
                var separatorIndex = name.LastIndexOf('.');
                if (separatorIndex >= 0)
                {
                    return name.Substring(separatorIndex + 1);
                }
                else
                {
                    return name;
                }
            }

            // Get name by convention
            if (componentType.Name.EndsWith(ViewComponentTypes.ViewComponentSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return componentType.Name.Substring(0, componentType.Name.Length - ViewComponentTypes.ViewComponentSuffix.Length);
            }
            else
            {
                return componentType.Name;
            }
        }
    }
}
