// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using UnityEngine;
using System;

using System.Linq;
using System.Collections.Generic;





#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sven.Utils
{
    /// <summary>
    /// Helper class to manage SVEN settings.
    /// </summary>
    public static class SvenSettings
    {
        #region UseInside
        public static bool UseInside => _useInside;
        private static bool _useInside = false;
        public static readonly string _useInsideKey = "SVEN_UseInside";
        #endregion

        #region Debug
        public static bool Debug => _debug;
        private static bool _debug = false;
        public static readonly string _debugKey = "SVEN_Debug";
        #endregion

        #region PointOfViewDebugColor
        public static Color PointOfViewDebugColor => _pointOfViewDebugColor;
        private static Color _pointOfViewDebugColor = Color.red;
        public static readonly string _pointOfViewDebugColorKey = "SVEN_PointOfViewDebugColor";
        #endregion

        #region PointerDebugColor
        public static Color PointerDebugColor => _pointerDebugColor;
        private static Color _pointerDebugColor = Color.blue;
        public static readonly string _pointerDebugColorKey = "SVEN_PointerDebugColor";
        #endregion

        #region GraspAreaDebugColor
        public static Color GraspAreaDebugColor => _graspAreaDebugColor;
        private static Color _graspAreaDebugColor = Color.green;
        public static readonly string _graspAreaDebugColorKey = "SVEN_GraspAreaDebugColor";
        #endregion

        #region EndpointUrl
        public static string EndpointUrl
        {
            get
            {
                if (_endpointUrl != null) return _endpointUrl;
                string argsEndpointUrl = Environment.GetCommandLineArgs().FirstOrDefault(arg => arg.StartsWith("--endpoint-url="))?.Split('=')[1];
                if (!string.IsNullOrEmpty(argsEndpointUrl) && Uri.TryCreate(argsEndpointUrl, UriKind.Absolute, out Uri parsedUri))
                    _endpointUrl = parsedUri.ToString();
                else
                    _endpointUrl = "http://localhost:7200/repositories/Demo-Scene";
                return _endpointUrl;
            }
            set
            {
                if (_endpointUrl == value) return;
                _endpointUrl = value;
            }
        }
        private static string _endpointUrl = null;
        public static readonly string _endpointUrlKey = "SVEN_EndpointUrl";
        #endregion

        #region SemanticizeFrequency
        public static int SemanticizeFrequency
        {
            get
            {
                if (0 < _semanticizeFrequency && _semanticizeFrequency <= 60) return _semanticizeFrequency;
                string argsSemanticizeFrequency = Environment.GetCommandLineArgs().FirstOrDefault(arg => arg.StartsWith("--semanticize-frequency="))?.Split('=')[1];
                if (int.TryParse(argsSemanticizeFrequency, out int parsedFrequency) && parsedFrequency > 0 && parsedFrequency <= 60)
                    _semanticizeFrequency = parsedFrequency;
                else
                    _semanticizeFrequency = 10;
                return _semanticizeFrequency;
            }
            set
            {
                if (_semanticizeFrequency == value) return;
                if (value < 1 || value > 60) throw new ArgumentOutOfRangeException(nameof(value), "Semanticize frequency must be between 1 and 60 seconds.");
                _semanticizeFrequency = value;
            }
        }
        private static int _semanticizeFrequency = 0;
        public static readonly string _semanticizeFrequencyKey = "SVEN_SemanticizeFrequency";
        #endregion

        #region Ontologies
        public static Dictionary<string, string> Ontologies
        {
            get
            {
                // load all .ttl files in StreamingAssets/Ontologies directory
                if (_ontologies.Count == 0)
                {
                    string[] ontologyFiles = System.IO.Directory.GetFiles(Application.streamingAssetsPath + "/Ontologies", "*.ttl");
                    foreach (string file in ontologyFiles)
                    {
                        string ontologyName = System.IO.Path.GetFileNameWithoutExtension(file);
                        string absolutePath = System.IO.Path.GetFullPath(file);
                        _ontologies[ontologyName] = absolutePath;
                    }
                }
                return _ontologies;
            }
        }
        private static Dictionary<string, string> _ontologies = new();
        public static readonly string _ontologiesKey = "SVEN_Ontologies";
        #endregion


#if UNITY_EDITOR
        public static void RefreshConfig()
        {
            try
            {
                _useInside = EditorPrefs.GetBool(_useInsideKey, UseInside);
                _debug = EditorPrefs.GetBool(_debugKey, Debug);
                _pointOfViewDebugColor = ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(_pointOfViewDebugColorKey, null), out Color pointOfViewDebugColor) ? pointOfViewDebugColor : PointOfViewDebugColor;
                _pointerDebugColor = ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(_pointerDebugColorKey, null), out Color pointerDebugColor) ? pointerDebugColor : PointerDebugColor;
                _graspAreaDebugColor = ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(_graspAreaDebugColorKey, null), out Color graspAreaDebugColor) ? graspAreaDebugColor : GraspAreaDebugColor;
                _endpointUrl = EditorPrefs.GetString(_endpointUrlKey, EndpointUrl);
                _semanticizeFrequency = EditorPrefs.GetInt(_semanticizeFrequencyKey, SemanticizeFrequency);
                _ontologies = Ontologies;
            }
            catch { }
        }

        static SvenSettings()
        {
            RefreshConfig();
        }
#endif
    }
}