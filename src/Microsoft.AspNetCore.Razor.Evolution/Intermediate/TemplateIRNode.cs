﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public class TemplateIRNode : ExtensionIRNode
    {
        public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

        public override RazorIRNode Parent { get; set; }

        public override SourceSpan? Source { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<TemplateIRNode>(this, visitor);
        }

        public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            return AcceptExtensionNode<TemplateIRNode, TResult>(this, visitor);
        }

        internal override void WriteNode(RuntimeTarget target, CSharpRenderingContext context)
        {
            var extension = target.GetExtension<ITemplateTargetExtension>();
            extension.WriteTemplate(context, this);
        }
    }
}
