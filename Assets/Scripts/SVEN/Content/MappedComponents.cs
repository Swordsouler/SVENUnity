using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SVEN.Content
{
    /// <summary>
    /// Mapped component to semantize.
    /// </summary>
    public static class MapppedComponents
    {
        /// <summary>
        /// Description of a property.
        /// </summary>
        public class PropertyDescription
        {
            /// <summary>
            /// Name of the property.
            /// </summary>
            public string PredicateName { get; set; }
            /// <summary>
            /// Getter of the property.
            /// </summary>
            public Func<object> Getter { get; set; }
            /// <summary>
            /// Setter of the property.
            /// </summary>
            public Action<object> Setter { get; set; }
            /// <summary>
            /// Simplified name of the property.
            /// </summary>
            public string SimplifiedName { get; set; }

            public PropertyDescription(string predicateName, Func<object> getter, Action<object> setter, string simplifiedName = "")
            {
                PredicateName = predicateName;
                Getter = getter;
                Setter = setter;
                SimplifiedName = simplifiedName;
            }
        }

        /// <summary>
        /// Values of the properties of the components.
        /// </summary>
        private static readonly Dictionary<Type, List<Delegate>> Value = new()
        {
            {
                typeof(Transform), new List<Delegate>
                {
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("position", () => transform.position, value => transform.position = (Vector3)value, "virtualPosition")),
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("rotation", () => transform.rotation, value => transform.rotation = (Quaternion)value, "virtualRotation")),
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("scale", () => transform.localScale, value => transform.localScale = (Vector3)value, "virtualSize")),
                }
            },
            {
                typeof(Renderer), new List<Delegate>
                {
                    (Func<Renderer, PropertyDescription>)(renderer => new PropertyDescription("enabled", () => renderer.enabled, value => renderer.enabled = (bool)value)),
                    (Func<Renderer, PropertyDescription>)(renderer => new PropertyDescription("isVisible", () => renderer.isVisible, null)),
                    (Func<Renderer, PropertyDescription>)(renderer => new PropertyDescription("color", () => renderer.material.color, value => renderer.material.color = (Color)value, "virtualColor")),
                    (Func<Renderer, PropertyDescription>)(renderer => new PropertyDescription("shader", () => renderer.material.shader.name, value => renderer.material.shader = Shader.Find((string)value))),
                }
            },
            {
                typeof(MeshFilter), new List<Delegate>
                {
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("triangles", () => string.Join("|", meshFilter.mesh.triangles.Select(t => t.ToString())), null)),
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("vertices", () => string.Join("|", meshFilter.mesh.vertices.Select(v => v.ToString())), null)),
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("normals", () => string.Join("|", meshFilter.mesh.normals.Select(n => n.ToString())), null)),
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("uvs", () => string.Join("|", meshFilter.mesh.uv.Select(uv => uv.ToString())), null)),
                }
            },
        };

        /// <summary>
        /// Add a component to the mapped components.
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <returns>True if the component was added, false otherwise.</returns>
        public static bool ContainsKey(Type type)
        {
            if (Value.ContainsKey(type))
                return true;
            else foreach (var key in Value.Keys)
                    if (key.IsAssignableFrom(type))
                        return true;
            return false;
        }

        /// <summary>
        /// Get the values of the properties of a component.
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <returns>List of properties of the component.</returns>
        public static List<Delegate> GetValue(Type type)
        {
            if (Value.TryGetValue(type, out var value))
                return value;
            else foreach (var key in Value.Keys)
                    if (key.IsAssignableFrom(type))
                        return Value[key];
            return null;
        }
    }
}