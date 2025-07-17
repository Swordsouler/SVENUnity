// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.Content;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Sven.GraphManagement
{
    public class GraphController : MonoBehaviour
    {
        private void Awake()
        {
            if (GraphManager.Count != 0) return;
            _ = GraphManager.Reload();
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
            await GraphManager.ApplyRulesAsync();
            await GraphManager.SaveToEndpoint();
        }

        public async void SaveAndQuitGraph()
        {
            await SaveGraph();
            GraphManager.Clear();
        }
    }
}