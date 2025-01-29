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

        /// <summary>
        /// The object that was collided.
        /// </summary>
        private SemantizationCore _receiver;

        public CollisionEvent(SemantizationCore sender, SemantizationCore receiver) : base(null)
        {
            _sender = sender;
            _receiver = receiver;
        }

        /// <summary>
        /// Semantizes the event.
        /// </summary>
        /// <param name="graph">The graph to semantize the event.</param>
        public new IUriNode Semantize(IGraph graph)
        {
            IUriNode eventNode = base.Semantize(graph);
            graph.Assert(new Triple(eventNode, graph.CreateUriNode("sven:sender"), graph.CreateUriNode("sven:" + _sender.GetUUID())));
            graph.Assert(new Triple(eventNode, graph.CreateUriNode("sven:receiver"), graph.CreateUriNode("sven:" + _receiver.GetUUID())));
            return eventNode;
        }
    }
}