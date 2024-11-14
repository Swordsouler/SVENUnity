#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using NaughtyAttributes.Editor;
using System.Reflection;

namespace SVEN
{
    /// <summary>
    /// Custom editor for the SemantizationCore component.
    /// </summary>
    [CustomEditor(typeof(SemantizationCore))]
    public class SemantizationCoreEditor : NaughtyInspector
    {
        /// <summary>
        /// Flag to show or hide the semantization foldout.
        /// </summary>
        private bool showSemantization = true;

        /// <summary>
        /// Override the default inspector GUI to display the semantization foldout.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Display default inspector elements with NaughtyAttributes
            base.OnInspectorGUI();

            // Get the instance of RDFCore
            SemantizationCore core = (SemantizationCore)target;

            // Display the foldout for semantization
            showSemantization = EditorGUILayout.Foldout(showSemantization, "Semantization", true);
            if (showSemantization)
            {
                // Get all components of the GameObject
                Component[] allComponents = core.GetComponents<Component>();

                // Display checkboxes for each component
                foreach (Component component in allComponents)
                {
                    // Exclude the RDFCore component from the list
                    if (component is SemantizationCore) continue;

                    // Check if the component has the Semantize method
                    MethodInfo semantizeMethod = typeof(SemantizationExtensions).GetMethod("GetProperties", new[] { component.GetType() });
                    if (semantizeMethod == null) continue;

                    // Check if the component is in the componentsToSemantize list
                    bool isSemantized = core.componentsToSemantize.Contains(component);
                    // Display a checkbox for the component
                    bool newIsSemantized = EditorGUILayout.Toggle(component.GetType().Name, isSemantized);

                    // Update the componentsToSemantize list based on the checkbox state
                    if (newIsSemantized && !isSemantized)
                    {
                        core.componentsToSemantize.Add(component);
                    }
                    else if (!newIsSemantized && isSemantized)
                    {
                        core.componentsToSemantize.Remove(component);
                    }
                }
            }

            // Apply changes
            if (GUI.changed) EditorUtility.SetDirty(core);
        }
    }
}
#endif