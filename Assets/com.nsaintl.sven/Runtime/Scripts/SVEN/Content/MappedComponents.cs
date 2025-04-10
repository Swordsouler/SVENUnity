using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sven.Context;
using Sven.GeoData;
using Sven.Utils;
using UnityEngine;

namespace Sven.Content
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
        private static readonly Dictionary<Type, ComponentDescription> Values = new()
        {
            {
                typeof(BoxCollider), new("geo:Feature",
                new List<Delegate>
                {
                    (Func<BoxCollider, PropertyDescription>)(collider => new PropertyDescription("enabled", () => collider.enabled, value => collider.enabled = value.ToString().ToLower() == "true", 1)),
                    (Func<BoxCollider, PropertyDescription>)(collider => new PropertyDescription("size", () => collider.size, value => collider.size = (Vector3)value, 1)),
                    (Func<BoxCollider, PropertyDescription>)(collider => new PropertyDescription("center", () => collider.center, value => collider.center = (Vector3)value, 1)),
                    (Func<BoxCollider, PropertyDescription>)(collider => new PropertyDescription("isTrigger", () => collider.isTrigger, value => collider.isTrigger = value.ToString().ToLower() == "true", 1)),
                    (Func<BoxCollider, PropertyDescription>)(collider => new PropertyDescription("geo:hasGeometry", () => new GeoWKT(collider.bounds), value => {}, 1)),
                })
            },
            {
                typeof(Transform), new("Transform",
                new List<Delegate>
                {
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("position", () => transform.position, value => transform.DOMove((Vector3)value, lerpSpeed), 1/*, "virtualPosition"*/)),
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("rotation", () => transform.rotation, value => transform.DORotateQuaternion((Quaternion)value, lerpSpeed), 1/*, "virtualRotation"*/)),
                    (Func<Transform, PropertyDescription>)(transform => new PropertyDescription("scale", () => transform.localScale, value => transform.DOScale((Vector3)value, lerpSpeed), 1/*, "virtualSize"*/)),
                })
            },
            {
                typeof(AudioSource), new("Audio",
                new List<Delegate>
                {
                    (Func<AudioSource, PropertyDescription>)(audioSource => new PropertyDescription("enabled", () => audioSource.enabled, value => audioSource.enabled = value.ToString().ToLower() == "true", 1)),
                    (Func<AudioSource, PropertyDescription>)(audioSource => new PropertyDescription("minAudioDistance", () => audioSource.minDistance, value => audioSource.minDistance = (float)value, 1/*, "minSoundDistance"*/)),
                    (Func<AudioSource, PropertyDescription>)(audioSource => new PropertyDescription("maxAudioDistance", () => audioSource.maxDistance, value => audioSource.maxDistance = (float)value, 1/*, "maxSoundDistance"*/)),
                })
            },
            {
                typeof(Camera), new("Camera",
                new List<Delegate>
                {
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("enabled", () => camera.enabled, value => camera.enabled = value.ToString().ToLower() == "true", 1)),
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("nearClipPlane", () => camera.nearClipPlane, value => camera.nearClipPlane = (float)value, 1/*, "POVNear"*/)),
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("farClipPlane", () => camera.farClipPlane, value => camera.farClipPlane = (float)value, 1/*, "POVFar"*/)),
                    (Func<Camera, PropertyDescription>)(camera => new PropertyDescription("fieldOfView", () => camera.fieldOfView, value => camera.fieldOfView = (float)value, 1/*, "POVFOV"*/)),
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
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("color", () => meshRenderer.material.color, value => meshRenderer.material.DOColor((Color)value, lerpSpeed), 1/*, "virtualColor"*/)),
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("material1", () => meshRenderer.materials.Length > 0 ? meshRenderer.materials[0].name.Replace(" (Instance)", "") : null, value => {
                        if (value == null) return;
                        Material[] materials = meshRenderer.materials;
                        if(materials.Length > 0 && materials[0] != null && materials[0].name == (string)value) return;
                        if (materials.Length < 1) Array.Resize(ref materials, 1);
                        materials[0] = Resources.Load<Material>($"Materials/{(string)value}");
                        meshRenderer.materials = materials;
                    }, 1)),
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("material2", () => meshRenderer.materials.Length > 1 ? meshRenderer.materials[1].name.Replace(" (Instance)", "") : null, value => {
                        if (value == null) return;
                        Material[] materials = meshRenderer.materials;
                        if (materials.Length > 1 && materials[1] != null && materials[1].name == (string)value) return;
                        if (materials.Length < 2) Array.Resize(ref materials, 2);
                        materials[1] = Resources.Load<Material>($"Materials/{(string)value}");
                        meshRenderer.materials = materials;
                    }, 1)),
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("material3", () => meshRenderer.materials.Length > 2 ? meshRenderer.materials[2].name.Replace(" (Instance)", "") : null, value => {
                        if (value == null) return;
                        Material[] materials = meshRenderer.materials;
                        if (materials.Length > 2 && materials[2] != null && materials[2].name == (string)value) return;
                        if (materials.Length < 3) Array.Resize(ref materials, 3);
                        materials[2] = Resources.Load<Material>($"Materials/{(string)value}");
                        meshRenderer.materials = materials;
                    }, 1)),
                    (Func<MeshRenderer, PropertyDescription>)(meshRenderer => new PropertyDescription("material4", () => meshRenderer.materials.Length > 3 ? meshRenderer.materials[3].name.Replace(" (Instance)", "") : null, value => {
                        if (value == null) return;
                        Material[] materials = meshRenderer.materials;
                        if (materials.Length > 3 && materials[3] != null && materials[3].name == (string)value) return;
                        if (materials.Length < 4) Array.Resize(ref materials, 4);
                        materials[3] = Resources.Load<Material>($"Materials/{(string)value}");
                        meshRenderer.materials = materials;
                    }, 1)),
                })
            },
            {
                typeof(SpriteRenderer), new("2DRender",
                new List<Delegate>
                {
                    (Func<SpriteRenderer, PropertyDescription>)(spriteRenderer => new PropertyDescription("enabled", () => spriteRenderer.enabled, value => spriteRenderer.enabled = value.ToString().ToLower() == "true", 1)),
                    (Func<SpriteRenderer, PropertyDescription>)(spriteRenderer => new PropertyDescription("color", () => spriteRenderer.color, value => spriteRenderer.color = (Color)value, 1/*, "virtualColor"*/)),
                    (Func<SpriteRenderer, PropertyDescription>)(spriteRenderer => new PropertyDescription("sprite", () => spriteRenderer.sprite.name, value => spriteRenderer.sprite = Resources.Load<Sprite>((string)value), 1)),
                })
            },
            {
                typeof(MeshFilter), new("Shape",
                new List<Delegate>
                {
                    (Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("mesh", () => meshFilter.mesh.name.Replace(" Instance", ""), value => {
                        if (value == null) return;
                        if (meshFilter.mesh != null && meshFilter.mesh.name == (string)value && meshFilter.mesh.uv.Length > 0) return;
                        Mesh mesh = Resources.Load<Mesh>($"Meshes/{(string)value}");
                        if (mesh == null) return;
                        meshFilter.mesh = mesh;
                    }, 1)),
                    /*(Func<MeshFilter, PropertyDescription>)(meshFilter => new PropertyDescription("vertices", () => string.Join("|", meshFilter.mesh.vertices.Select(v => v.ToString())), value => {
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
                        }, 2)),*/
                })
            },
            {typeof(ManipulableObject), new("ManipulableObject",
                new List<Delegate>
                {
                    (Func<ManipulableObject, PropertyDescription>)(manipulableObject => new PropertyDescription("enabled", () => manipulableObject.enabled, value => manipulableObject.enabled = value.ToString().ToLower() == "true", 1)),
                })},

            // Ignore the following components
            {typeof(PointOfView), null},
            {typeof(GraspArea), new("GraspArea", new List<Delegate>
            {
                (Func<GraspArea, PropertyDescription>)(graspArea => new PropertyDescription("enabled", () => graspArea.enabled, value => graspArea.enabled = value.ToString().ToLower() == "true", 1)),
                (Func<GraspArea, PropertyDescription>)(graspArea => new PropertyDescription("graspDistance", () => graspArea.GraspDistance, value => graspArea.GraspDistance = (float)value, 1)),
            })},
            {typeof(Pointer), new("Pointer", new List<Delegate>
            {
                (Func<Pointer, PropertyDescription>)(pointer => new PropertyDescription("enabled", () => pointer.enabled, value => pointer.enabled = value.ToString().ToLower() == "true", 1)),
                (Func<Pointer, PropertyDescription>)(pointer => new PropertyDescription("pointerDistance", () => pointer.PointerDistance, value => pointer.PointerDistance = (float)value, 1)),
            })},
        };

        /// <summary>
        /// Adds a new component description to the dictionary.
        /// </summary>
        /// <param name="type">The type of the component.</param>
        /// <param name="description">The component description.</param>
        public static void AddComponentDescription(Type type, ComponentDescription description)
        {
            if (!Values.ContainsKey(type))
            {
                Values[type] = description;
            }
        }

        /// <summary>
        /// Update check for mesh filters.
        /// </summary>
        private static readonly Dictionary<MeshFilter, MeshFilterUpdate> _meshFilterUpdates = new();

        /// <summary>
        /// Mesh filter update.
        /// </summary>
        private class MeshFilterUpdate
        {
            /// <summary>
            /// Check if the vertices were updated.
            /// </summary>
            public bool Vertices { get; set; } = false;
            /// <summary>
            /// Check if the triangles were updated.
            /// </summary>
            public bool Triangles { get; set; } = false;
            /// <summary>
            /// Check if the normals were updated.
            /// </summary>
            public bool Normals { get; set; } = false;
            /// <summary>
            /// Check if the UVs were updated.
            /// </summary>
            public bool UVs { get; set; } = false;

            /// <summary>
            /// Check if the mesh filter update is complete.
            /// </summary>
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
            if (Values.TryGetValue(component.GetType(), out var componentDescription))
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
            if (Values.ContainsKey(type))
                return true;
            else foreach (var key in Values.Keys)
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
            if (Values.TryGetValue(type, out var value))
                return value;
            else foreach (Type key in Values.Keys)
                    if (key.IsAssignableFrom(type))
                        return Values[key];
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
            if (Values.TryGetValue(type, out componentDescription))
                return true;
            else foreach (Type key in Values.Keys)
                    if (key.IsAssignableFrom(type))
                    {
                        componentDescription = Values[key];
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
            if (Values.TryGetValue(type, out var componentDescription))
                return componentDescription.CachedProperties.ContainsKey(propertyName);
            else foreach (Type key in Values.Keys)
                    if (key.IsAssignableFrom(type))
                        return Values[key].CachedProperties.ContainsKey(propertyName);
            return false;
        }

        /// <summary>
        /// Get the type of the component.
        /// </summary>
        /// <param name="typeName">Name of the component.</param>
        /// <returns>Type of the component.</returns>
        public static Type GetType(string typeName)
        {
            try
            {
                foreach (var key in Values.Keys)
                {
                    string typeNameKey = Values[key].TypeName.Contains(":") ? Values[key].TypeName.Split(':')[1] : Values[key].TypeName;
                    if (typeNameKey == typeName)
                        return key;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}