// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.Content;
using VDS.RDF;

namespace Sven.Context
{
    public class CollisionEvent : Event
    {
        /// <summary>
        /// The object that collided.
        /// </summary>
        private SemantizationCore _sender;
        private string _senderUUID;

        /// <summary>
        /// The object that was collided.
        /// </summary>
        private SemantizationCore _receiver;
        private string _receiverUUID;

        public CollisionEvent(SemantizationCore sender, SemantizationCore receiver) : base(null)
        {
            _sender = sender;
            _senderUUID = sender.GetUUID();
            _receiver = receiver;
            _receiverUUID = receiver.GetUUID();
        }

        /// <summary>
        /// Semantizes the event.
        /// </summary>
        /// <param name="graph">The graph to semantize the event.</param>
        public new IUriNode Semantize(IGraph graph)
        {
            IUriNode eventNode = base.Semantize(graph);
            graph.Assert(new Triple(eventNode, graph.CreateUriNode("sven:sender"), graph.CreateUriNode("sven:" + _senderUUID)));
            graph.Assert(new Triple(eventNode, graph.CreateUriNode("sven:receiver"), graph.CreateUriNode("sven:" + _receiverUUID)));
            return eventNode;
        }
    }
}