using System;
using System.Text;
using NaughtyAttributes;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Writing;
using VDS.RDF.Parsing;

namespace RDF
{
    [DisallowMultipleComponent]
    public class GraphBehaviour : MonoBehaviour
    {
        #region Flags

        /// <summary>
        /// Flag to check if the graph is started.
        /// </summary>
        protected bool IsStarted => graph != null;

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Graph to store the RDF data.
        /// </summary>
        protected Graph graph;
        /// <summary>
        /// Graph to store the RDF data.
        /// </summary>
        public Graph Graph => graph;

        #endregion

        #region Graph Actions

        /// <summary>
        /// Decode the graph to a turtle string.
        /// </summary>
        /// <param name="graph">The graph to decode.</param>
        /// <returns>Decoded graph in turtle format.</returns>
        protected string DecodeGraph(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph) + " is null.");
            StringBuilder sb = new();
            CompressingTurtleWriter writer = new(TurtleSyntax.Rdf11Star);
            writer.Save(graph, new System.IO.StringWriter(sb));
            return sb.ToString();
        }

        /// <summary>
        /// Print the turtle of the graph in the console.
        /// </summary>
        [Button("Print"), ShowIf(EConditionOperator.And, "IsStarted", "IsLocal")]
        private void Print()
        {
            Debug.Log(DecodeGraph(Graph));
        }

        #endregion
    }
}