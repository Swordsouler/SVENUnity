using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
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
            /// <summary>
            /// Priority of the property. (closer to 0 is higher priority)
            /// </summary>
            public int Priority { get; set; }

            public PropertyDescription(string predicateName, Func<object> getter, Action<object> setter, int priority, string simplifiedName = "")
            {
                PredicateName = predicateName;
                Getter = getter;
                Setter = setter;
                SimplifiedName = simplifiedName;
                Priority = priority;
            }

            public PropertyDescription(string predicateName, Func<object> getter, Action<object> setter, int priority)
            {
                PredicateName = predicateName;
                Getter = getter;
                Setter = setter;
                SimplifiedName = "";
                Priority = priority;
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

        private static readonly float lerpSpeed = 0.4f;

        /// <summary>
        /// Values of the properties of the components.
        /// </summary>
        private static readonly Dictionary<Type, ComponentDescription> Value = new()
        {
            {
                typeof(Transform), new("Transform",
                new List<Delegate>
                {
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("position", () => transform.position, value => transform.DOMove((Vector3)value, lerpSpeed), 1, "virtualPosition")),
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("rotation", () => transform.rotation, value => transform.DORotateQuaternion((Quaternion)value, lerpSpeed), 1, "virtualRotation")),
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("scale", () => transform.localScale, value => transform.DOScale((Vector3)value, lerpSpeed), 1, "virtualSize")),
                })
            },
            {
                typeof(AudioSource), new("Audio",
                new List<Delegate>
                {
                    (Func<AudioSource, PropertyDescription>)(audioSource => new PropertyDescription("enabled", () => audioSource.enabled, value => audioSource.enabled = value.ToString().ToLower() == "true", 1)),
                    (Func<AudioSource, PropertyDescription>)(audioSource => new PropertyDescription("minAudioDistance", () => audioSource.minDistance, value => audioSource.minDistance = (float)value, 1, "minSoundDistance")),
                    (Func<AudioSource, PropertyDescription>)(audioSource => new PropertyDescription("maxAudioDistance", () => audioSource.maxDistance, value => audioSource.maxDistance = (float)value, 1, "maxSoundDistance")),
                })
            },
            {
                typeof(Camera), new("Camera",
                new List<Delegate>
                {
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("enabled", () => camera.enabled, value => camera.enabled = value.ToString().ToLower() == "true", 1)),
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("nearClipPlane", () => camera.nearClipPlane, value => camera.nearClipPlane = (float)value, 1, "POVNear")),
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("farClipPlane", () => camera.farClipPlane, value => camera.farClipPlane = (float)value, 1, "POVFar")),
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("fieldOfView", () => camera.fieldOfView, value => camera.fieldOfView = (float)value, 1, "POVFOV")),
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("orthographic", () => camera.orthographic, value => camera.orthographic = value.ToString() == "true", 1)),
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("orthographicSize", () => camera.orthographicSize, value => camera.orthographicSize = (float)value, 1)),
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("clearFlags", () => camera.clearFlags.ToString(), value => camera.clearFlags = (CameraClearFlags)Enum.Parse(typeof(CameraClearFlags), (string)value), 1)),
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("backgroundColor", () => camera.backgroundColor, value => camera.backgroundColor = (Color)value, 1)),
                })
            },
            {
                typeof(MeshRenderer), new("3DRender",
                new List<Delegate>
                {
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("enabled", () => meshRenderer.enabled, value => meshRenderer.enabled = value.ToString().ToLower() == "true", 1)),
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("color", () => meshRenderer.material.color, value => meshRenderer.material.DOColor((Color)value, lerpSpeed), 1, "virtualColor")),
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("shader", () => meshRenderer.material.shader.name, value => meshRenderer.material.shader = Shader.Find((string)value), 1)),
                })
            },
            {
                typeof(SpriteRenderer), new("2DRender",
                new List<Delegate>
                {
                    (Func<SpriteRenderer, PropertyDescription>)(spriteRenderer => new PropertyDescription("enabled", () => spriteRenderer.enabled, value => spriteRenderer.enabled = value.ToString().ToLower() == "true", 1)),
                    (Func<SpriteRenderer, PropertyDescription>)(spriteRenderer => new PropertyDescription("color", () => spriteRenderer.color, value => spriteRenderer.color = (Color)value, 1, "virtualColor")),
                    (Func<SpriteRenderer, PropertyDescription>)(spriteRenderer => new PropertyDescription("sprite", () => spriteRenderer.sprite.name, value => spriteRenderer.sprite = Resources.Load<Sprite>((string)value), 1)),
                })
            },
            {
                typeof(MeshFilter), new("Shape",
                new List<Delegate>
                {
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("vertices", () => string.Join("|", meshFilter.mesh.vertices.Select(v => v.ToString())), value => {
                            try {
                                Vector3[] vertices = ((string)value).Split('|').Select(ParseVector3).ToArray();
                                if (vertices.Length == meshFilter.mesh.vertexCount) return;
                                meshFilter.mesh.Clear();
                                meshFilter.mesh.SetVertices(vertices);
                                _meshFilterUpdates[meshFilter] = new() { Vertices = true };
                            } catch {}
                        }, 1)),
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("triangles", () => string.Join("|", meshFilter.mesh.triangles.Select(t => t.ToString())), value => {
                            try {
                                if(!_meshFilterUpdates.TryGetValue(meshFilter, out MeshFilterUpdate update) || update.Triangles) return;

                                int[] triangles = ((string)value).Split('|').Select(int.Parse).ToArray();
                                meshFilter.mesh.SetTriangles(triangles, 0);
                                update.Triangles = true;
                                if(update.IsComplete) _meshFilterUpdates.Remove(meshFilter);
                            } catch {}
                        }, 2)),
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("normals", () => string.Join("|", meshFilter.mesh.normals.Select(n => n.ToString())), value => {
                            try {
                                if(!_meshFilterUpdates.TryGetValue(meshFilter, out MeshFilterUpdate update) || update.Normals) return;

                                Vector3[] normals = ((string)value).Split('|').Select(ParseVector3).ToArray();
                                if (normals.Length != meshFilter.mesh.vertexCount) return;
                                meshFilter.mesh.SetNormals(normals);
                                update.Normals = true;
                                if(update.IsComplete) _meshFilterUpdates.Remove(meshFilter);
                            } catch {}
                        }, 2)),
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("uvs", () => string.Join("|", meshFilter.mesh.uv.Select(uv => uv.ToString())), value => {
                            try {
                                if(!_meshFilterUpdates.TryGetValue(meshFilter, out MeshFilterUpdate update) || update.UVs) return;

                                if((string)value == "") {
                                    meshFilter.mesh.SetUVs(0, new List<Vector2>(new Vector2[meshFilter.mesh.vertexCount]));
                                } else {
                                    Vector2[] uvs = ((string)value).Split('|').Select(ParseVector2).ToArray();
                                    meshFilter.mesh.SetUVs(0, uvs);
                                }
                                update.UVs = true;
                                if(update.IsComplete) _meshFilterUpdates.Remove(meshFilter);
                            } catch {}
                        }, 2)),
                })
            },
        };

        private static readonly Dictionary<MeshFilter, MeshFilterUpdate> _meshFilterUpdates = new();

        private class MeshFilterUpdate
        {
            public bool Vertices { get; set; } = false;
            public bool Triangles { get; set; } = false;
            public bool Normals { get; set; } = false;
            public bool UVs { get; set; } = false;

            public bool IsComplete => Vertices && Triangles && Normals && UVs;
        }

        /// <summary>
        /// Parse a Vector3 from a string.
        /// </summary>
        /// <param name="value">String to parse. (0, 1, 2)
        /// <returns>Vector3 parsed.</returns> 
        private static Vector3 ParseVector3(string value)
        {
            string[] values = value.Replace("(", "").Replace(")", "").Replace(" ", "").Split(',');
            if (values.Length < 3)
                return Vector3.zero;
            return new Vector3(float.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Parse a Vector2 from a string.
        /// </summary>
        /// <param name="value">String to parse. (0, 1)
        /// <returns>Vector2 parsed.</returns>
        private static Vector2 ParseVector2(string value)
        {
            string[] values = value.Replace("(", "").Replace(")", "").Replace(" ", "").Split(',');
            if (values.Length < 2)
                return Vector2.zero;
            return new Vector2(float.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Get the properties of a component.
        /// </summary>
        /// <param name="component">Component to get the properties from.</param>
        /// <returns>Dictionary of properties of the component.</returns>
        public static Dictionary<string, Tuple<int, Action<object>>> GetSetters(Component component)
        {
            Dictionary<string, Tuple<int, Action<object>>> setters = new();
            if (Value.TryGetValue(component.GetType(), out var componentDescription))
                foreach (Delegate del in componentDescription.Properties)
                    if (del.DynamicInvoke(component) is PropertyDescription propertyDescription)
                        setters.Add(propertyDescription.PredicateName, new(propertyDescription.Priority, propertyDescription.Setter));
            return setters;
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