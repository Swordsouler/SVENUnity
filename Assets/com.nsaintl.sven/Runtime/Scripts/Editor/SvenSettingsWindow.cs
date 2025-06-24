// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#if UNITY_EDITOR
using Sven.Utils;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sven.Editor
{

    /// <summary>
    /// Helper window to manage SVEN more easily.
    /// </summary>
    public class SvenSettingsWindow : EditorWindow
    {
        private readonly Dictionary<string, Vector2> ontologiesScrollPosition = new();
        private readonly Dictionary<string, bool> ontologiesIsShown = new();

        [MenuItem("Window/SVEN Settings")]
        public static void ShowWindow()
        {
            GetWindow<SvenSettingsWindow>("SVEN Settings");
        }

        private void OnLostFocus()
        {
            ontologiesIsShown.Clear();
            ontologiesScrollPosition.Clear();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("SVEN Settings", EditorStyles.boldLabel);

            bool refresh = false;

            bool useInside = SvenSettings.UseInside;
            bool newUseInside = EditorGUILayout.Toggle("Use inside", useInside);

            if (newUseInside != useInside)
            {
                EditorPrefs.SetBool(SvenSettings._useInsideKey, newUseInside);
                refresh = true;
            }

            bool debug = SvenSettings.Debug;
            bool newDebug = EditorGUILayout.Toggle("Show debug logs", debug);

            if (newDebug != debug)
            {
                EditorPrefs.SetBool(SvenSettings._debugKey, newDebug);
                refresh = true;
            }

            Color pointOfViewDebugColor = SvenSettings.PointOfViewDebugColor;
            Color newPointOfViewDebugColor = EditorGUILayout.ColorField("Point of View Debug Color", pointOfViewDebugColor);

            if (newPointOfViewDebugColor != pointOfViewDebugColor)
            {
                EditorPrefs.SetString(SvenSettings._pointOfViewDebugColorKey, ColorUtility.ToHtmlStringRGB(newPointOfViewDebugColor));
                refresh = true;
            }

            Color pointerDebugColor = SvenSettings.PointerDebugColor;
            Color newPointerDebugColor = EditorGUILayout.ColorField("Pointer Debug Color", pointerDebugColor);

            if (newPointerDebugColor != pointerDebugColor)
            {
                EditorPrefs.SetString(SvenSettings._pointerDebugColorKey, ColorUtility.ToHtmlStringRGB(newPointerDebugColor));
                refresh = true;
            }

            Color graspAreaDebugColor = SvenSettings.GraspAreaDebugColor;
            Color newGraspAreaDebugColor = EditorGUILayout.ColorField("Grasp Area Debug Color", graspAreaDebugColor);

            if (newGraspAreaDebugColor != graspAreaDebugColor)
            {
                EditorPrefs.SetString(SvenSettings._graspAreaDebugColorKey, ColorUtility.ToHtmlStringRGB(newGraspAreaDebugColor));
                refresh = true;
            }

            string endpointUrl = SvenSettings.EndpointUrl;
            string newEndpointUrl = EditorGUILayout.TextField("Endpoint URL", endpointUrl);
            if (newEndpointUrl != endpointUrl)
            {
                SvenSettings.EndpointUrl = newEndpointUrl;
                EditorPrefs.SetString(SvenSettings._endpointUrlKey, newEndpointUrl);
                refresh = true;
            }

            int semanticizeFrequency = SvenSettings.SemanticizeFrequency;
            int newSemanticizeFrequency = EditorGUILayout.IntSlider("Semanticize Frequency (record/second)", semanticizeFrequency, 1, 60);
            if (newSemanticizeFrequency != semanticizeFrequency)
            {
                SvenSettings.SemanticizeFrequency = newSemanticizeFrequency;
                EditorPrefs.SetInt(SvenSettings._semanticizeFrequencyKey, newSemanticizeFrequency);
                refresh = true;
            }

            // ontologies
            EditorGUILayout.LabelField("Ontologies", EditorStyles.boldLabel);
            // create a copy
            var ontologies = new Dictionary<string, string>(SvenSettings.Ontologies);
            foreach (var ontology in ontologies)
            {
                // ontology value is scrollable with a scrollbar, readonly, and has a fixed height of 100 pixels
                EditorGUILayout.BeginHorizontal("GroupBox");
                EditorGUILayout.BeginVertical(GUILayout.Width(200));
                EditorGUILayout.LabelField(ontology.Key, GUILayout.Width(125));
                // remove button for ontology
                if (GUILayout.Button("Remove", GUILayout.ExpandWidth(true)))
                {
                    // delete the files in StreamingAssets/Ontologies/<ontology.Key>.ttl
                    string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "Ontologies", ontology.Key + ".ttl");
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

                    SvenSettings.Ontologies.Clear();
                    SvenSettings.RefreshConfig();
                }
                EditorGUILayout.EndVertical();
                if (ontologiesIsShown.ContainsKey(ontology.Key) && ontologiesIsShown[ontology.Key])
                {
                    // scrollable labelfield for ontology value
                    if (!ontologiesScrollPosition.ContainsKey(ontology.Key))
                    {
                        ontologiesScrollPosition[ontology.Key] = Vector2.zero;
                    }
                    ontologiesScrollPosition[ontology.Key] = EditorGUILayout.BeginScrollView(ontologiesScrollPosition[ontology.Key], GUILayout.Height(150));
                    // read the content of the file at <ontology.Value>
                    string ontologyContent = System.IO.File.Exists(ontology.Value) ? System.IO.File.ReadAllText(ontology.Value) : "File not found: " + ontology.Value;
                    GUILayout.TextArea(ontologyContent, EditorStyles.textField, GUILayout.ExpandHeight(true));
                    GUILayout.EndScrollView();
                }
                else
                {
                    // button show content of ontology file
                    if (GUILayout.Button("Show Content", GUILayout.ExpandWidth(true), GUILayout.Height(39)))
                    {
                        if (ontologiesIsShown.ContainsKey(ontology.Key))
                            ontologiesIsShown[ontology.Key] = !ontologiesIsShown[ontology.Key];
                        else
                            ontologiesIsShown[ontology.Key] = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Refresh Ontologies"))
            {
                SvenSettings.Ontologies.Clear();
                SvenSettings.RefreshConfig();
            }
            // import ontology button (select .ttl file, copy it into StreamingAssets/Ontologies, and refresh Ontologies)
            if (GUILayout.Button("Import Ontology"))
            {
                string path = EditorUtility.OpenFilePanel("Select Ontology File", Application.streamingAssetsPath + "/Ontologies", "ttl");
                if (!string.IsNullOrEmpty(path))
                {
                    string fileName = System.IO.Path.GetFileName(path);
                    string destinationPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Ontologies", fileName);
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destinationPath));
                    System.IO.File.Copy(path, destinationPath, true);
                    SvenSettings.Ontologies.Clear();
                    SvenSettings.RefreshConfig();
                }
            }
            if (GUILayout.Button("Open Ontologies Folder"))
            {
                string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Ontologies");
                if (System.IO.Directory.Exists(path))
                {
                    EditorUtility.RevealInFinder(path);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "The Ontologies folder does not exist. Please create it first.", "OK");
                }
            }

            if (refresh) SvenSettings.RefreshConfig();
        }
    }
}
#endif