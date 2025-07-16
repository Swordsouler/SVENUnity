// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Sven.Content
{
    public interface IComponentMapping
    {
        public abstract string SemanticTypeName { get; }
        public static ComponentMapping ComponentMapping() => throw new NotImplementedException();
    }
}