using System.Collections.Generic;
using UnityEngine;

namespace Sven.GraphManagement
{
    /// <summary>
    /// GraphManager class to manage the graph buffers.
    /// </summary>
    public class GraphManager
    {
        /// <summary>
        /// Cached graph buffers.
        /// </summary>
        private static readonly Dictionary<string, GraphBuffer> cachedGraphBuffers = new();

        /// <summary>
        /// Get the graph from graph name.
        /// </summary>
        /// <param name="graphName">Name of the graph.</param>
        /// <returns>GraphBuffer instance.</returns>
        public static GraphBuffer Get(string graphName)
        {
            if (cachedGraphBuffers.ContainsKey(graphName) && cachedGraphBuffers[graphName] != null)
                return cachedGraphBuffers[graphName];

            // search in scene a graphBuffer with the same name
            GraphBuffer[] graphBuffers = Object.FindObjectsByType<GraphBuffer>(FindObjectsSortMode.None);
            foreach (GraphBuffer graphBuffer in graphBuffers)
            {
                cachedGraphBuffers[graphBuffer.GraphName] = graphBuffer;
                if (graphBuffer.GraphName.ToLower() == graphName.ToLower())
                    return graphBuffer;
            }
            return null;
        }
    }
}