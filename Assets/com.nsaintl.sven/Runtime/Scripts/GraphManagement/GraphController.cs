// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NaughtyAttributes;
using Sven.Content;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Sven.GraphManagement
{
    public class GraphController : MonoBehaviour
    {
        [ShowNativeProperty] private string BaseUri => "https://sven.lisn.upsaclay.fr/ve/" + _graphName + "/";
        [SerializeField] private string _graphName = "Default";
        public string GraphName
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

        private void Awake()
        {
            if (GraphManager.Count != 0) return;
            GraphManager.Clear();
            GraphManager.LoadOntologies();
            GraphManager.SetBaseUri(BaseUri);
            GraphManager.SetNamespace("", BaseUri);
        }

        public async Task SaveGraph()
        {
            SemantizationCore[] semantizationCores = FindObjectsByType<SemantizationCore>(FindObjectsSortMode.None);
            SynchronizationContext context = SynchronizationContext.Current;
#if !UNITY_WEBGL || UNITY_EDITOR
            await Task.Run(() =>
            {
#endif
                foreach (SemantizationCore semantizationCore in semantizationCores)
                    context.Send(_ => semantizationCore.OnDestroy(), null);
#if !UNITY_WEBGL || UNITY_EDITOR
            });
#endif
            GraphManager.ApplyRules();
            await GraphManager.SaveToEndpoint();
        }

        public async void SaveAndQuitGraph()
        {
            await SaveGraph();
            GraphManager.Clear();
        }
    }
}