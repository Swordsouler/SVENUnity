using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace SVEN.Content
{
    public static class SemantizationExtensions
    {
        #region UUID

        /// <summary>
        /// Generates a unique identifier for the component.
        /// </summary>
        /// <returns>Unique identifier.</returns>
        private static readonly Dictionary<Component, string> componentUUIDs = new();

        /// <summary>
        /// Generates a unique identifier for the component.
        /// </summary>
        /// <param name="component">Component to generate the identifier.</param>
        private static void GenerateUUID(this Component component)
        {
            componentUUIDs[component] = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Destroys the identifier for the component. WARNING: Not doing this will cause memory leaks.
        /// </summary>
        /// <param name="component">Component to destroy the identifier.</param>
        public static void DestroyUUID(this Component component)
        {
            if (componentUUIDs.ContainsKey(component))
                componentUUIDs.Remove(component);
        }

        /// <summary>
        /// Gets the identifier for the component.
        /// </summary>
        /// <param name="component">Component to get the identifier.</param>
        /// <returns>Unique identifier.</returns>
        public static string GetUUID(this Component component)
        {
            try
            {
                if (!componentUUIDs.ContainsKey(component)) component.GenerateUUID();
                return componentUUIDs[component];
            }
            catch (KeyNotFoundException e)
            {
                Debug.LogError(e);
                return "";
            }
        }

        #endregion

        #region Observers

        private static readonly List<string> ignoredProperties = new()
        {
            "destroyCancellationToken",
            "useGUILayout",
            "runInEditMode",
            "isActiveAndEnabled",
            "hideFlags",
            "didStart",
            "didAwake",
            "name"
        };

        private static List<Property> GetAllProperties(this Component component)
        {
            List<Property> properties = new();
            foreach (var field in component.GetType().GetProperties())
            {
                if (field.DeclaringType == typeof(Component)) continue;
                if (ignoredProperties.Contains(field.Name)) continue;
                properties.Add(new Property(field.Name, () => field.GetValue(component)));
            }
            return properties;
        }

        public static List<Property> GetProperties(this Transform transform)
        {
            Vector3 position = transform.position;
            Vector3 rotation = transform.eulerAngles;
            Vector3 scale = transform.localScale;

            List<Property> observers = new()
            {
                new Property("position", () => position),
                new Property("rotation", () => rotation),
                new Property("scale", () => scale),
            };

            return observers;
        }

        /*public static List<Property> GetObservers(this Renderer meshRenderer)
        {
            return meshRenderer.GetAllProperties();
        }*/

        public static List<Property> GetProperties(this Atom atom)
        {
            return atom.GetAllProperties();
        }

        #endregion

        #region Semantize

        public static void Semantize(this Component component, IGraph graph)
        {

        }

        #endregion

        #region Data Types

        /// <summary>
        /// Get the XML Schema data type for the object.
        /// </summary>
        /// <param name="obj">Object to get the XML Schema data type.</param>
        /// <returns>XML Schema data type.</returns>
        public static string GetXmlSchemaTypes(this object obj)
        {
            Type type = obj.GetType();
            return type switch
            {
                Type t when t == typeof(bool) => XmlSpecsHelper.XmlSchemaDataTypeBoolean,
                Type t when t == typeof(int) => XmlSpecsHelper.XmlSchemaDataTypeInt,
                Type t when t == typeof(float) => XmlSpecsHelper.XmlSchemaDataTypeFloat,
                _ => XmlSpecsHelper.XmlSchemaDataTypeString,
            };
        }

        /// <summary>
        /// Get the XML Schema data type for the object.
        /// </summary>
        private static Dictionary<Type, List<string>> SupportedNestedTypes { get; } = new()
        {
            { typeof(Vector3), new List<string> { "x", "y", "z" } },
            { typeof(Vector2), new List<string> { "x", "y" } },
            { typeof(Color), new List<string> { "r", "g", "b", "a" } },
        };

        /// <summary>
        /// Get the nested values of the object.
        /// </summary>
        /// <param name="obj">Object to get the nested values.</param>
        /// <returns>Nested values.</returns>
        public static Dictionary<string, object> GetSemantizableValues(this object obj)
        {
            Dictionary<string, object> values = new();
            Type type = obj.GetType();
            if (type.IsPrimitive || type == typeof(string))
            {
                values.Add("value", obj);

            }
            else
            {
                if (!SupportedNestedTypes.ContainsKey(type))
                {
                    Debug.LogWarning($"Type {type} is not supported for nested values. Returning the object as a string.");
                    values.Add("value", obj.ToString());
                    return values;
                }
                foreach (string fieldName in SupportedNestedTypes[type])
                {
                    FieldInfo field = type.GetField(fieldName);
                    if (field != null)
                    {
                        values.Add(fieldName, field.GetValue(obj));
                    }
                    else
                    {
                        PropertyInfo property = type.GetProperty(fieldName);
                        if (property != null)
                        {
                            values.Add(fieldName, property.GetValue(obj));
                        }
                    }
                }
            }
            return values;
        }

        #endregion
    }
}