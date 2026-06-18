// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.Content;
using Sven.Utils;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Sven.GraphManagement
{
    [DisallowMultipleComponent, AddComponentMenu("SVEN/Graph Controller")]
    public class GraphController : MonoBehaviour
    {
        private void Awake()
        {
            // Capture les chemins Application.* sur le main thread, avant tout accès depuis un Task.Run.
            SvenSettings.CacheMainThreadPaths();
            if (GraphManager.Count != 0) return;
            _ = GraphManager.Reload();
        }

        /// <summary>
        /// Forces a final flush of the in-memory buffer when the application quits (standalone quit, Alt-F4,
        /// headset menu quit, or exiting Play Mode in the editor). Without this, every triple accumulated since
        /// the last successful flush — up to BufferSize — would be lost. Blocks briefly (bounded by a timeout);
        /// if the endpoint is unreachable, the buffer is written to a local backup instead of being lost.
        /// </summary>
        private void OnApplicationQuit()
        {
            GraphManager.ForceFlushToEndpointBlocking();
        }

        public async Task SaveGraphToEndpoint()
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
            await GraphManager.AddToEndpoint();
        }

        public async Task SaveGraphToFile()
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
            // save to streaming assets persistent data path
            string path = Application.streamingAssetsPath;
            await GraphManager.SaveToFile(path);
        }

        public async void SaveAndQuitGraph()
        {
            await SaveGraphToEndpoint();
            //await SaveGraphToFile();
            GraphManager.Clear();
        }

        public async void SaveAndQuitGraphToEndpoint()
        {
            await SaveGraphToEndpoint();
            GraphManager.Clear();
        }

        public async void SaveAndQuitGraphToFile()
        {
            await SaveGraphToFile();
            GraphManager.Clear();
        }
    }
}