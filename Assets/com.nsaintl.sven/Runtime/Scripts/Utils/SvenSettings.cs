// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using NaughtyAttributes;

using Sven.GraphManagement;

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
                string argsEndpointUrl = Environment.GetCommandLineArgs().FirstOrDefault(arg => arg.StartsWith("--sven-endpoint-url="))?.Split('=')[1];
                if (!string.IsNullOrEmpty(argsEndpointUrl) && Uri.TryCreate(argsEndpointUrl, UriKind.Absolute, out Uri parsedUri))
                    _endpointUrl = parsedUri.ToString();
                else
                    _endpointUrl = "http://localhost:7200/repositories/SVEN";
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

        #region Username
        public static string Username
        {
            get
            {
                if (_username != null) return _username;
                string argsUsername = Environment.GetCommandLineArgs().FirstOrDefault(arg => arg.StartsWith("--sven-username="))?.Split('=')[1];
                if (!string.IsNullOrEmpty(argsUsername))
                    _username = argsUsername;
                else
                    _username = "admin";
                return _username;
            }
            set
            {
                if (_username == value) return;
                _username = value;
            }
        }
        private static string _username = null;
        public static readonly string _usernameKey = "SVEN_Username";
        #endregion

        #region Password
        public static string Password
        {
            get
            {
                if (_password != null) return _password;
                string argsPassword = Environment.GetCommandLineArgs().FirstOrDefault(arg => arg.StartsWith("--sven-password="))?.Split('=')[1];
                if (!string.IsNullOrEmpty(argsPassword))
                    _password = argsPassword;
                else
                    _password = "admin";
                return _password;
            }
            set
            {
                if (_password == value) return;
                _password = value;
            }
        }
        private static string _password = null;
        public static readonly string _passwordKey = "SVEN_Password";
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

                if (_ontologies.Count == 0)
                {
                    string ontologiesPath = Application.streamingAssetsPath + "/Ontologies";
                    if (System.IO.Directory.Exists(ontologiesPath))
                    {
                        string[] ontologyFiles = System.IO.Directory.GetFiles(ontologiesPath, "*.ttl");
                        foreach (string file in ontologyFiles)
                        {
                            string ontologyName = System.IO.Path.GetFileNameWithoutExtension(file);
                            string absolutePath = System.IO.Path.GetFullPath(file);
                            _ontologies[ontologyName] = absolutePath;
                        }
                    }
                    else
                    {
                        System.IO.Directory.CreateDirectory(ontologiesPath);

                        string svenTtlPath = System.IO.Path.Combine(ontologiesPath, "sven.ttl");
                        try
                        {
                            using (var client = new System.Net.WebClient())
                            {
                                client.DownloadFile("https://sven.lisn.upsaclay.fr/ontology#", svenTtlPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError("Failed to download SVEN ontology: " + ex.Message);
                            System.IO.File.WriteAllText(svenTtlPath, "# SVEN ontology\n# Voir : https://sven.lisn.upsaclay.fr/ontology#\n");
                        }

                        _ontologies["sven"] = System.IO.Path.GetFullPath(svenTtlPath);
                    }
                }
                return _ontologies;
            }
        }
        private static Dictionary<string, string> _ontologies = new();
        public static readonly string _ontologiesKey = "SVEN_Ontologies";
        #endregion

        #region BaseUri


        [ShowNativeProperty] public static string BaseUri => "https://sven.lisn.upsaclay.fr/ve/" + _graphName + "/";
        [SerializeField] private static string _graphName = "Default";
        public static readonly string _graphNameKey = "SVEN_GraphName";
        public static string GraphName
        {
            get => _graphName;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                if (_graphName == value) return;
                _graphName = value;
                GraphManager.SetBaseUri(BaseUri);
                GraphManager.SetNamespace("", BaseUri);
            }
        }

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
                _username = EditorPrefs.GetString(_usernameKey, Username);
                _password = EditorPrefs.GetString(_passwordKey, Password);
                _semanticizeFrequency = EditorPrefs.GetInt(_semanticizeFrequencyKey, SemanticizeFrequency);
                _ontologies = Ontologies;
                _graphName = EditorPrefs.GetString(_graphNameKey, GraphName);
            }
            catch { }
        }

        static SvenSettings()
        {
            RefreshConfig();
        }
#endif

        public static async Task<Dictionary<string, string>> GetOntologiesAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string indexPath = Application.streamingAssetsPath + "/Ontologies/ontologies_index.json";
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(indexPath))
            {
                await request.SendWebRequest();
                if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    UnityEngine.Debug.LogError("Failed to load ontologies index: " + request.error);
                    return new Dictionary<string, string>();
                }
                var index = JsonUtility.FromJson<OntologyIndex>(request.downloadHandler.text);
                var dict = new Dictionary<string, string>();
                foreach (var file in index.files)
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(file);
                    string url = Application.streamingAssetsPath + "/Ontologies/" + file;
                    dict[name] = url;
                }
                return dict;
            }
#else
            return await Task.Run(() =>
            {
                if (_ontologies.Count == 0)
                {
                    string ontologiesPath = Application.streamingAssetsPath + "/Ontologies";
                    if (System.IO.Directory.Exists(ontologiesPath))
                    {
                        string[] ontologyFiles = System.IO.Directory.GetFiles(ontologiesPath, "*.ttl");
                        foreach (string file in ontologyFiles)
                        {
                            string ontologyName = System.IO.Path.GetFileNameWithoutExtension(file);
                            string absolutePath = System.IO.Path.GetFullPath(file);
                            _ontologies[ontologyName] = absolutePath;
                        }
                    }
                }
                return _ontologies;
            });
#endif
        }

        [System.Serializable]
        private class OntologyIndex
        {
            public string[] files;
        }
    }
}