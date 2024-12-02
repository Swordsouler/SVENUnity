#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using NaughtyAttributes.Editor;
using System.Reflection;
using SVEN.Content;

namespace SVEN.Editor
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

                // Disable GUI if the editor is in play mode
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

                // Display checkboxes for each component
                foreach (Component component in allComponents)
                {
                    // Exclude the RDFCore component from the list
                    if (component is SemantizationCore) continue;

                    // Check if the component has the Semantize method
                    bool hasGetProperties = MapppedComponents.ContainsKey(component.GetType());

                    // Check if the component is in the componentsToSemantize list
                    bool isSemantized = core.componentsToSemantize.Contains(component);

                    // Create a GUIStyle for the label
                    GUIStyle labelStyle = new(EditorStyles.label);
                    if (!hasGetProperties)
                    {
                        labelStyle.normal.textColor = Color.yellow;
                    }

                    // Display a checkbox for the component with "(ALL)" and a tooltip if it doesn't have GetProperties
                    string label = component.GetType().Name + (hasGetProperties ? "" : " (ALL)");
                    GUIContent content = new(label, "The component will be fully semantized, which may cause performance issues. To fix this, declare a GetProperties function for this component in SemantizationExtensions.");
                    bool newIsSemantized = EditorGUILayout.ToggleLeft(content, isSemantized, labelStyle);

                    // Update the componentsToSemantize list based on the checkbox state
                    if (newIsSemantized && !isSemantized)
                    {
                        core.componentsToSemantize.Add(component);
                        core.componentsToSemantize.RemoveAll(c => c == null);
                    }
                    else if (!newIsSemantized && isSemantized)
                    {
                        core.componentsToSemantize.Remove(component);
                        core.componentsToSemantize.RemoveAll(c => c == null);
                    }
                }

                // Re-enable GUI
                EditorGUI.EndDisabledGroup();
            }

            // Apply changes
            if (GUI.changed) EditorUtility.SetDirty(core);
        }
    }
}
#endif