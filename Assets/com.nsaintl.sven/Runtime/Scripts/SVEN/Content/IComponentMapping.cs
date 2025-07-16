// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Sven.Content
{
    public interface IComponentMapping
    {
        public static string SemanticTypeName => throw new NotImplementedException();
        public static ComponentMapping ComponentMapping() => throw new NotImplementedException();
        public static Type GetType(string semanticTypeName)
        {
            // get all IComponentMapping implementations and find the one with the matching SemanticTypeName == semancTypeName
            var mappings = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IComponentMapping).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
            foreach (var mapping in mappings)
            {
                // Get the SemanticTypeName property value
                var semanticTypeNameProperty = mapping.GetProperty("SemanticTypeName");
                if (semanticTypeNameProperty != null)
                {
                    var semanticTypeNameValue = semanticTypeNameProperty.GetValue(null) as string;
                    if (semanticTypeNameValue == semanticTypeName)
                    {
                        return mapping;
                    }
                }
            }
            throw new ArgumentException($"No IComponentMapping found for semantic type name: {semanticTypeName}");
        }
    }
}