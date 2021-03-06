﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.CodeGeneration;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    // These tests cover the methods on ExtensionIRNode that are used to implement visitors
    // that special case an extension node.
    public class ExtensionIRNodeTest
    {
        [Fact]
        public void Accept_CallsStandardVisitExtension_ForStandardVisitor()
        {
            // Arrange
            var node = new TestExtensionIRNode();
            var visitor = new StandardVisitor();

            // Act
            node.Accept(visitor);

            // Assert
            Assert.True(visitor.WasStandardMethodCalled);
            Assert.False(visitor.WasSpecificMethodCalled);
        }

        [Fact]
        public void Accept_CallsSpecialVisitExtension_ForSpecialVisitor()
        {
            // Arrange
            var node = new TestExtensionIRNode();
            var visitor = new SpecialVisitor();

            // Act
            node.Accept(visitor);

            // Assert
            Assert.False(visitor.WasStandardMethodCalled);
            Assert.True(visitor.WasSpecificMethodCalled);
        }

        [Fact]
        public void Accept_TResult_CallsStandardVisitExtension_ForStandardVisitor()
        {
            // Arrange
            var node = new TestExtensionIRNode();
            var visitor = new StandardVisitor<bool>();

            // Act
            node.Accept(visitor);

            // Assert
            Assert.True(visitor.WasStandardMethodCalled);
            Assert.False(visitor.WasSpecificMethodCalled);
        }

        [Fact]
        public void Accept_TResult_CallsSpecialVisitExtension_ForSpecialVisitor()
        {
            // Arrange
            var node = new TestExtensionIRNode();
            var visitor = new SpecialVisitor<bool>();

            // Act
            node.Accept(visitor);

            // Assert
            Assert.False(visitor.WasStandardMethodCalled);
            Assert.True(visitor.WasSpecificMethodCalled);
        }

        private class TestExtensionIRNode : ExtensionIRNode
        {
            public override IList<RazorIRNode> Children => throw new NotImplementedException();

            public override RazorIRNode Parent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override SourceSpan? Source { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Accept(RazorIRNodeVisitor visitor)
            {
                // This is the standard visitor boilerplate for an extension node.
                AcceptExtensionNode<TestExtensionIRNode>(this, visitor);
            }

            public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
            {
                // This is the standard visitor boilerplate for an extension node.
                return AcceptExtensionNode<TestExtensionIRNode, TResult>(this, visitor);
            }

            internal override void WriteNode(RuntimeTarget target, CSharpRenderingContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class StandardVisitor : RazorIRNodeVisitor
        {
            public bool WasStandardMethodCalled { get; private set; }
            public bool WasSpecificMethodCalled { get; private set; }

            public override void VisitExtension(ExtensionIRNode node)
            {
                WasStandardMethodCalled = true;
            }

            public void VisitExtension(TestExtensionIRNode node)
            {
                WasSpecificMethodCalled = true;
            }
        }

        private class StandardVisitor<T> : RazorIRNodeVisitor<T>
        {
            public bool WasStandardMethodCalled { get; private set; }
            public bool WasSpecificMethodCalled { get; private set; }

            public override T VisitExtension(ExtensionIRNode node)
            {
                WasStandardMethodCalled = true;
                return default(T);
            }

            public T VisitExtension(TestExtensionIRNode node)
            {
                WasSpecificMethodCalled = true;
                return default(T);
            }
        }

        private class SpecialVisitor : RazorIRNodeVisitor, IExtensionIRNodeVisitor<TestExtensionIRNode>
        {
            public bool WasStandardMethodCalled { get; private set; }
            public bool WasSpecificMethodCalled { get; private set; }

            public override void VisitExtension(ExtensionIRNode node)
            {
                WasStandardMethodCalled = true;
            }

            public void VisitExtension(TestExtensionIRNode node)
            {
                WasSpecificMethodCalled = true;
            }
        }

        private class SpecialVisitor<T> : RazorIRNodeVisitor<T>, IExtensionIRNodeVisitor<TestExtensionIRNode, T>
        {
            public bool WasStandardMethodCalled { get; private set; }
            public bool WasSpecificMethodCalled { get; private set; }

            public override T VisitExtension(ExtensionIRNode node)
            {
                WasStandardMethodCalled = true;
                return default(T);
            }

            public T VisitExtension(TestExtensionIRNode node)
            {
                WasSpecificMethodCalled = true;
                return default(T);
            }
        }
    }
}
