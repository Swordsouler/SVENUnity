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
        /// Description of a component.
        /// </summary>
        public class ComponentDescription
        {
            /// <summary>
            /// Name of the component in the knowledge graph.
            /// </summary>
            public string TypeName { get; set; }
            /// <summary>
            /// Delegates of the properties of the component.
            /// </summary>
            /// <value></value>
            public List<Delegate> Properties { get; set; }
            public Dictionary<string, PropertyDescription> CachedProperties { get; set; }

            public ComponentDescription(string typeName, List<Delegate> properties)
            {
                TypeName = typeName;
                Properties = properties;
                CachedProperties = new();
                foreach (Delegate property in properties)
                {
                    Type propertyType = property.GetType().GetGenericArguments().FirstOrDefault();
                    if (propertyType != null)
                    {
                        object instance = null;
                        try
                        {
                            instance = Activator.CreateInstance(propertyType, nonPublic: true);
                        }
                        catch
                        {
                            Debug.LogError("Error creating instance of " + propertyType.Name);
                        }

                        if (property.DynamicInvoke(instance) is PropertyDescription propertyDescription)
                            CachedProperties.Add(propertyDescription.PredicateName, propertyDescription);
                    }
                }
            }
        }

        /// <summary>
        /// Values of the properties of the components.
        /// </summary>
        private static readonly Dictionary<Type, ComponentDescription> Value = new()
        {
            {
                typeof(Transform), new("Transform",
                new List<Delegate>
                {
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("position", () => transform.position, value => transform.position = (Vector3)value, "virtualPosition")),
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("rotation", () => transform.rotation, value => transform.rotation = (Quaternion)value, "virtualRotation")),
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("scale", () => transform.localScale, value => transform.localScale = (Vector3)value, "virtualSize")),
                })
            },
            {
                typeof(MeshRenderer), new("MeshRenderer",
                new List<Delegate>
                {
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("enabled", () => meshRenderer.enabled, value => meshRenderer.enabled = (bool)value)),
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("isVisible", () => meshRenderer.isVisible, null)),
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("color", () => meshRenderer.material.color, value => meshRenderer.material.color = (Color)value, "virtualColor")),
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("shader", () => meshRenderer.material.shader.name, value => meshRenderer.material.shader = Shader.Find((string)value))),
                })
            },
            {
                typeof(MeshFilter), new("Shape",
                new List<Delegate>
                {
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("triangles", () => string.Join("|", meshFilter.mesh.triangles.Select(t => t.ToString())), value => meshFilter.mesh.SetTriangles(((string)value).Split('|').Select(int.Parse).ToArray(), 0))),
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("vertices", () => string.Join("|", meshFilter.mesh.vertices.Select(v => v.ToString())), value => meshFilter.mesh.SetVertices(((string)value).Split('|').Select(ParseVector3).ToArray()))),
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("normals", () => string.Join("|", meshFilter.mesh.normals.Select(n => n.ToString())), value => meshFilter.mesh.SetNormals(((string)value).Split('|').Select(ParseVector3).ToArray()))),
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("uvs", () => string.Join("|", meshFilter.mesh.uv.Select(uv => uv.ToString())), value => meshFilter.mesh.SetUVs(0, ((string)value).Split('|').Select(ParseVector2).ToArray()))),
                })
            },
        };

        /// <summary>
        /// Parse a Vector3 from a string.
        /// </summary>
        /// <param name="value">String to parse. (0, 1, 2)
        /// <returns>Vector3 parsed.</returns> 
        private static Vector3 ParseVector3(string value)
        {
            string[] values = value.Split(',');
            return new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
        }

        /// <summary>
        /// Parse a Vector2 from a string.
        /// </summary>
        /// <param name="value">String to parse. (0, 1)
        /// <returns>Vector2 parsed.</returns>
        private static Vector2 ParseVector2(string value)
        {
            string[] values = value.Split(',');
            return new Vector2(float.Parse(values[0]), float.Parse(values[1]));
        }

        /// <summary>
        /// Check if a component is mapped.
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <returns>True if the component is mapped, false otherwise.</returns>
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
        public static ComponentDescription GetValue(Type type)
        {
            if (Value.TryGetValue(type, out var value))
                return value;
            else foreach (Type key in Value.Keys)
                    if (key.IsAssignableFrom(type))
                        return Value[key];
            return null;
        }

        /// <summary>
        /// Get the values of the component description.
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <param name="componentDescription">Component description.</param>
        /// <returns>True if the component was found, false otherwise.</returns> 
        public static bool TryGetValue(Type type, out ComponentDescription componentDescription)
        {
            if (Value.TryGetValue(type, out componentDescription))
                return true;
            else foreach (Type key in Value.Keys)
                    if (key.IsAssignableFrom(type))
                    {
                        componentDescription = Value[key];
                        return true;
                    }
            return false;
        }

        /// <summary>
        /// Check if a component has a property.
        /// </summary>
        /// <param name="type">Type of the component.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>True if the component has the property, false otherwise.</returns>
        public static bool HasProperty(Type type, string propertyName)
        {
            if (Value.TryGetValue(type, out var componentDescription))
                return componentDescription.CachedProperties.ContainsKey(propertyName);
            else foreach (Type key in Value.Keys)
                    if (key.IsAssignableFrom(type))
                        return Value[key].CachedProperties.ContainsKey(propertyName);
            return false;
        }

        /// <summary>
        /// Get the type of the component.
        /// </summary>
        /// <param name="typeName">Name of the component.</param>
        /// <returns>Type of the component.</returns>
        public static Type GetType(string typeName)
        {
            foreach (var key in Value.Keys)
                if (Value[key].TypeName == typeName)
                    return key;
            return null;
        }
    }
}