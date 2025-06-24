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
    public class SvenConfigWindow : EditorWindow
    {
        private readonly Dictionary<string, Vector2> ontologiesScrollPosition = new();
        private readonly Dictionary<string, bool> ontologiesIsShown = new();

        [MenuItem("Tools/SVEN Helper")]
        public static void ShowWindow()
        {
            GetWindow<SvenConfigWindow>("SVEN Helper");
        }

        private void OnLostFocus()
        {
            ontologiesIsShown.Clear();
            ontologiesScrollPosition.Clear();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("SVEN Helper", EditorStyles.boldLabel);

            bool refresh = false;

            bool useInside = SvenConfig.UseInside;
            bool newUseInside = EditorGUILayout.Toggle("Use inside", useInside);

            if (newUseInside != useInside)
            {
                EditorPrefs.SetBool(SvenConfig._useInsideKey, newUseInside);
                refresh = true;
            }

            bool debug = SvenConfig.Debug;
            bool newDebug = EditorGUILayout.Toggle("Show debug logs", debug);

            if (newDebug != debug)
            {
                EditorPrefs.SetBool(SvenConfig._debugKey, newDebug);
                refresh = true;
            }

            Color pointOfViewDebugColor = SvenConfig.PointOfViewDebugColor;
            Color newPointOfViewDebugColor = EditorGUILayout.ColorField("Point of View Debug Color", pointOfViewDebugColor);

            if (newPointOfViewDebugColor != pointOfViewDebugColor)
            {
                EditorPrefs.SetString(SvenConfig._pointOfViewDebugColorKey, ColorUtility.ToHtmlStringRGB(newPointOfViewDebugColor));
                refresh = true;
            }

            Color pointerDebugColor = SvenConfig.PointerDebugColor;
            Color newPointerDebugColor = EditorGUILayout.ColorField("Pointer Debug Color", pointerDebugColor);

            if (newPointerDebugColor != pointerDebugColor)
            {
                EditorPrefs.SetString(SvenConfig._pointerDebugColorKey, ColorUtility.ToHtmlStringRGB(newPointerDebugColor));
                refresh = true;
            }

            Color graspAreaDebugColor = SvenConfig.GraspAreaDebugColor;
            Color newGraspAreaDebugColor = EditorGUILayout.ColorField("Grasp Area Debug Color", graspAreaDebugColor);

            if (newGraspAreaDebugColor != graspAreaDebugColor)
            {
                EditorPrefs.SetString(SvenConfig._graspAreaDebugColorKey, ColorUtility.ToHtmlStringRGB(newGraspAreaDebugColor));
                refresh = true;
            }

            string endpointUrl = SvenConfig.EndpointUrl;
            string newEndpointUrl = EditorGUILayout.TextField("Endpoint URL", endpointUrl);
            if (newEndpointUrl != endpointUrl)
            {
                SvenConfig.EndpointUrl = newEndpointUrl;
                EditorPrefs.SetString(SvenConfig._endpointUrlKey, newEndpointUrl);
                refresh = true;
            }

            int semanticizeFrequency = SvenConfig.SemanticizeFrequency;
            int newSemanticizeFrequency = EditorGUILayout.IntSlider("Semanticize Frequency (record/second)", semanticizeFrequency, 1, 60);
            if (newSemanticizeFrequency != semanticizeFrequency)
            {
                SvenConfig.SemanticizeFrequency = newSemanticizeFrequency;
                EditorPrefs.SetInt(SvenConfig._semanticizeFrequencyKey, newSemanticizeFrequency);
                refresh = true;
            }

            // ontologies
            EditorGUILayout.LabelField("Ontologies", EditorStyles.boldLabel);
            // create a copy
            var ontologies = new Dictionary<string, string>(SvenConfig.Ontologies);
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

                    SvenConfig.Ontologies.Clear();
                    SvenConfig.RefreshConfig();
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
                SvenConfig.Ontologies.Clear();
                SvenConfig.RefreshConfig();
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
                    SvenConfig.Ontologies.Clear();
                    SvenConfig.RefreshConfig();
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

            if (refresh) SvenConfig.RefreshConfig();
        }
    }
}
#endif