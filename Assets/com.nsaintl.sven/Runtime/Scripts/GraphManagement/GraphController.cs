// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Sven.GraphManagement
{
    public class GraphController : MonoBehaviour
    {
        [SerializeField] private string _baseUri = "https://sven.lisn.upsaclay.fr/Default/";
        public string BaseUri
        {
            get => _baseUri;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                if (_baseUri == value) return;
                _baseUri = value;
                GraphManager.SetBaseUri(BaseUri);
            }
        }

        private void Awake()
        {
            GraphManager.Clear();
            LoadOntologies();
            GraphManager.SetBaseUri(BaseUri);
        }

        private void LoadOntologies()
        {
            Dictionary<string, string> ontologies = SvenConfig.Ontologies;
            foreach (KeyValuePair<string, string> ontology in ontologies)
                GraphManager.AddOntology(ontology.Key, ontology.Value);
        }

        private void Update()
        {
            //Debug.Log(GraphManager.BaseUri);
        }

        private void OnApplicationQuit()
        {
            GraphManager.Clear();
        }
    }
}