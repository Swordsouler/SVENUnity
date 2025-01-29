using Sven.OwlTime;
using VDS.RDF;

namespace Sven.Context
{
    public class Event
    {
        /// <summary>
        /// The time at which the event occurred.
        /// </summary>
        private Interval _interval;

        /// <summary>
        /// The unique identifier of the event.
        /// </summary>
        private readonly string _uuid;

        /// <summary>
        /// The user that triggered the event.
        /// </summary>
        protected User _user;

        /// <summary>
        /// Gets the URI of the event.
        /// </summary>
        /// <param name="graph">The graph to get the URI from.</param>
        /// <returns>The URI of the event.</returns>
        public IUriNode GetUriNode(IGraph graph)
        {
            return graph.CreateUriNode("sven:" + _uuid);
        }

        public Event(User user)
        {
            _user = user;
            _interval = new Interval();
            _uuid = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Starts the event.
        /// </summary>
        /// <param name="instant">The instant at which the event started.</param>
        public void Start(Instant instant)
        {
            _interval.Start(instant);
        }

        /// <summary>
        /// Ends the event.
        /// </summary>
        /// <param name="instant">The instant at which the event ended.</param>
        public void End(Instant instant)
        {
            _interval.End(instant);
        }

        /// <summary>
        /// Semantizes the event.
        /// </summary>
        /// <param name="graph">The graph to semantize the event.</param>
        public IUriNode Semantize(IGraph graph)
        {
            IUriNode eventNode = GetUriNode(graph);
            graph.Assert(new Triple(eventNode, graph.CreateUriNode("rdf:type"), graph.CreateUriNode($"sven:{GetType().Name}")));
            graph.Assert(new Triple(eventNode, graph.CreateUriNode("time:hasTemporalExtent"), _interval.Semantize(graph)));
            if (_user != null) graph.Assert(new Triple(graph.CreateUriNode("sven:" + _user.UUID), graph.CreateUriNode("sven:perform"), eventNode));
            return eventNode;
        }
    }
}