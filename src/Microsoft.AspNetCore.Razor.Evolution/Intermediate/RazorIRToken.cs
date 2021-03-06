﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public sealed class RazorIRToken : RazorIRNode
    {
        public override IList<RazorIRNode> Children => RazorIRNode.EmptyArray;

        public string Content { get; set; }

        public bool IsCSharp => Kind == TokenKind.CSharp;

        public bool IsHtml => Kind == TokenKind.Html;

        public TokenKind Kind { get; set; } = TokenKind.Unknown;

        public override RazorIRNode Parent { get; set; }

        public override SourceSpan? Source { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitToken(this);
        }

        public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            return visitor.VisitToken(this);
        }

        public enum TokenKind
        {
            Unknown,
            CSharp,
            Html,
        }
    }
}


