using UnityEditor;
using UnityEngine;
using NaughtyAttributes.Editor;
using System;
using Sven.Content;
using static Sven.Content.SemantizationCore;

namespace Sven.Editor
{
    /// <summary>
    /// Custom editor for the SemantizationCore component.
    /// </summary>
    [CustomEditor(typeof(SemantizationCore))]
    //[CanEditMultipleObjects]
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

                // Get the names of the SemanticProcessingMode enum values
                string[] processingModeOptions = Enum.GetNames(typeof(SemanticProcessingMode));

                // Display checkboxes and dropdowns for each component
                foreach (Component component in allComponents)
                {
                    // Exclude the RDFCore component from the list
                    if (component is SemantizationCore) continue;

                    // Check if the component has the Semantize method
                    bool hasGetProperties = MapppedComponents.ContainsKey(component.GetType());

                    // Check if the component is in the componentsToSemantize list
                    bool isSemantized = core.componentsToSemantize.Find(c => c.Component == component) != null;

                    string label = component.GetType().Name;

                    // Create a GUIStyle for the label
                    GUIStyle labelStyle = new(EditorStyles.label);
                    if (!hasGetProperties)
                    {
                        labelStyle.normal.textColor = Color.yellow;
                        label = $"{label} (All properties will be semantized)";
                    }
                    else
                    {
                        string rdfType = MapppedComponents.GetValue(component.GetType()).TypeName;
                        if (rdfType != null && rdfType != label)
                            label = $"{label} ({MapppedComponents.GetValue(component.GetType()).TypeName})";
                    }

                    // Begin horizontal layout
                    EditorGUILayout.BeginHorizontal();

                    // Display a checkbox for the component with "(ALL)" and a tooltip if it doesn't have GetProperties
                    GUIContent content = new("", "The component will be fully semantized, which may cause performance issues. To fix this, declare a GetProperties function for this component in SemantizationExtensions.");
                    bool newIsSemantized = EditorGUILayout.Toggle(isSemantized, GUILayout.Width(20));

                    // Disable the dropdown if the checkbox is not checked
                    EditorGUI.BeginDisabledGroup(!newIsSemantized);

                    // Display a dropdown for the component with the options from SemanticProcessingMode
                    int selectedIndex = EditorGUILayout.Popup(newIsSemantized ? core.componentsToSemantize.Find(c => c.Component == component)?.ProcessingMode == SemanticProcessingMode.Static ? 1 : 0 : 0, newIsSemantized ? processingModeOptions : new string[] { "" }, GUILayout.Width(80));

                    // End disabled group for dropdown
                    EditorGUI.EndDisabledGroup();

                    // Display the label
                    GUILayout.Label(label, labelStyle);

                    // End horizontal layout
                    EditorGUILayout.EndHorizontal();

                    // Update the componentsToSemantize list based on the checkbox state
                    if (newIsSemantized && !isSemantized)
                    {
                        core.componentsToSemantize.Add(new SemanticComponent { Component = component, ProcessingMode = (SemanticProcessingMode)selectedIndex });
                        core.componentsToSemantize.RemoveAll(c => c == null || c.Component == null || !component.gameObject.Equals(c.Component.gameObject));
                    }
                    else if (!newIsSemantized && isSemantized)
                    {
                        core.componentsToSemantize.Remove(core.componentsToSemantize.Find(c => c.Component == component));
                        core.componentsToSemantize.RemoveAll(c => c == null || c.Component == null || !component.gameObject.Equals(c.Component.gameObject));
                    }

                    // Handle the dropdown selection (you can add your logic here)
                    if (newIsSemantized)
                    {
                        core.componentsToSemantize.Find(c => c.Component == component).ProcessingMode = (SemanticProcessingMode)selectedIndex;
                    }
                }

                // Re-enable GUI
                EditorGUI.EndDisabledGroup();
            }

            // Apply changes
            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(core);
            }
        }
    }
}