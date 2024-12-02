using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SVEN.Content
{
    /// <summary>
    /// Mapped properties to semantize.
    /// </summary>
    public static class MapppedProperties
    {
        /// <summary>
        /// Description of a property.
        /// </summary>
        public class PropertyDescription
        {
            /// <summary>
            /// Name of the property.
            /// </summary>
            public string TypeName { get; set; }
            /// <summary>
            /// Nested properties of the property.
            /// </summary>
            public List<string> NestedProperties { get; set; }

            public PropertyDescription(string typeName, List<string> nestedProperties)
            {
                TypeName = typeName;
                NestedProperties = nestedProperties;
            }
        }

        /// <summary>
        /// Values of the properties of the components.
        /// </summary>
        private static Dictionary<Type, PropertyDescription> Value { get; } = new()
        {
            { typeof(Matrix4x4), new PropertyDescription("Matrix4x4", new List<string> { "m00", "m01", "m02", "m03", "m10", "m11", "m12", "m13", "m20", "m21", "m22", "m23", "m30", "m31", "m32", "m33" }) },
            { typeof(Quaternion), new PropertyDescription("Quaternion", new List<string> { "x", "y", "z", "w" }) },
            { typeof(Vector4), new PropertyDescription("Vector4", new List<string> { "x", "y", "z", "w" }) },
            { typeof(Vector3), new PropertyDescription("Vector3", new List<string> { "x", "y", "z" }) },
            { typeof(Vector2), new PropertyDescription("Vector2", new List<string> { "x", "y" }) },
            { typeof(Color), new PropertyDescription("Color", new List<string> { "r", "g", "b", "a" }) },
        };

        /// <summary>
        /// Add a component to the mapped components.
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <returns>True if the component was added, false otherwise.</returns>
        public static bool ContainsKey(Type type)
        {
            return Value.ContainsKey(type);
        }

        /// <summary>
        /// Get the values of the properties of a component.
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <returns>List of properties of the component.</returns>
        public static PropertyDescription GetValue(Type type)
        {
            if (Value.TryGetValue(type, out var value))
                return value;
            return new PropertyDescription("Field", new List<string> { "value" });
        }
    }
}