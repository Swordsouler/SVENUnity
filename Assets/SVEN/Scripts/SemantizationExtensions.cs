using System.Collections.Generic;
using UnityEngine;

namespace SVEN
{
    public static class SemantizationExtensions
    {
        #region ResourceID

        /// <summary>
        /// Generates a unique resource identifier for the component.
        /// </summary>
        /// <returns>Unique resource identifier.</returns>
        private static readonly Dictionary<Component, string> componentResourceIDs = new();

        /// <summary>
        /// Generates a unique resource identifier for the component.
        /// </summary>
        /// <param name="component">Component to generate the resource identifier.</param>
        public static void GenerateResourceID(this Component component)
        {
            componentResourceIDs[component] = System.Guid.NewGuid().ToString();
        }

        public static void DestroyResourceID(this Component component)
        {
            componentResourceIDs.Remove(component);
        }

        /// <summary>
        /// Gets the resource identifier for the component.
        /// </summary>
        /// <param name="component">Component to get the resource identifier.</param>
        /// <returns>Resource identifier.</returns>
        public static string ResourceID(this Component component)
        {
            try
            {
                string resourceID = (string)component.GetType().GetProperty("ResourceID")?.GetValue(component);
                return resourceID ?? componentResourceIDs[component];
            }
            catch (KeyNotFoundException e)
            {
                Debug.LogError(e);
                return "";
            }
        }

        /// <summary>
        /// Gets the resource for the component.
        /// </summary>
        /// <param name="component">Component to get the resource.</param>
        /// <returns>Resource.</returns>
        public static string Resource(this Component component)
        {
            // with prefixe
            return component.ResourceID();
        }

        #endregion

        #region XSD Data
        public static string ToXSDData(this string value)
        {
            return $"\"{value}\"^^xsd:string";
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
            transform.GenerateResourceID();
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
            meshRenderer.GenerateResourceID();
            return meshRenderer.GetAllProperties();
        }*/

        public static List<Property> GetProperties(this Atom atom)
        {
            atom.GenerateResourceID();
            return atom.GetAllProperties();
        }

        #endregion
    }
}