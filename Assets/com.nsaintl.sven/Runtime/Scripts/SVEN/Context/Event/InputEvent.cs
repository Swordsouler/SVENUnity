// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using VDS.RDF;

namespace Sven.Context
{
    public class InputEvent : Event
    {
        /// <summary>
        /// The input that triggered the event.
        /// </summary>
        private string _input;

        public InputEvent(User user, string input) : base(user)
        {
            _input = input;
        }

        /// <summary>
        /// Semantizes the event.
        /// </summary>
        /// <param name="graph">The graph to semantize the event.</param>
        public new IUriNode Semantize(IGraph graph)
        {
            IUriNode eventNode = base.Semantize(graph);
            graph.Assert(new Triple(eventNode, graph.CreateUriNode("sven:input"), graph.CreateLiteralNode(_input)));
            return eventNode;
        }
    }
}