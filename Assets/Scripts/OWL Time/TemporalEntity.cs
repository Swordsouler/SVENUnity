using System;
using System.Diagnostics;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace OWLTime
{
    /// <summary>
    /// Represents a temporal entity with a beginning and an end.
    /// </summary>
    public class TemporalEntity
    {
        /// <summary>
        /// The prefix of the temporal entity.
        /// </summary>
        private readonly string prefix = "time:";
        private readonly string UUID;
        public string GetUUID()
        {
            return UUID;
        }

        /// <summary>
        /// The temporal entity that occurs before this one
        /// </summary>
        private TemporalEntity after;

        /// <summary>
        // The temporal entity that occurs after this one
        /// </summary>
        private TemporalEntity before;

        /// <summary>
        // The beginning instant of this temporal entity
        /// </summary>
        public Instant hasBeginning;

        /// <summary>
        // The end instant of this temporal entity
        /// </summary>
        public Instant hasEnd;

        /// <summary>
        // The duration of this temporal entity
        /// </summary>
        private XSDDuration hasXSDDuration;

        /// <summary>
        /// Initializes a new instance of the TemporalEntity class with the current date.
        /// </summary>
        /// <returns></returns>
        public TemporalEntity() : this(DateTime.Now) { }

        /// <summary>
        /// Initializes a new instance of the TemporalEntity class with the specified UUID.
        /// </summary>
        /// <param name="UUID">The UUID to initialize the instance with.</param>
        public TemporalEntity(string UUID)
        {
            this.UUID = UUID;
        }

        /// <summary>
        /// Initializes a new instance of the TemporalEntity class with the specified UUID.
        /// </summary>
        /// <param name="UUID">The UUID to initialize the instance with.</param>
        public TemporalEntity(string prefix, string UUID)
        {
            this.prefix = prefix;
            this.UUID = UUID;
        }

        /// <summary>
        /// Initializes a new instance of the TemporalEntity class with the specified date.
        /// </summary>
        /// <param name="date">The date to initialize the instance with.</param>
        public TemporalEntity(DateTime dateTime)
        {
            UUID = dateTime.ToString("yyyy-MM-dd-HH-mm-ss-fff");
        }

        /// <summary>
        /// Returns the UUID of the temporal entity.
        /// </summary>
        /// <returns>The UUID of the temporal entity. </returns> 
        public override string ToString()
        {
            return UUID;
        }

        /// <summary>
        /// Semantizes the temporal entity in the graph.
        /// </summary>
        /// <param name="graph">The graph to semantize the temporal entity.</param>
        public IUriNode Semantize(IGraph graph)
        {
            IUriNode temporalEntityNode = graph.CreateUriNode(prefix + UUID);
            graph.Assert(new Triple(temporalEntityNode, graph.CreateUriNode("rdf:type"), graph.CreateUriNode($"time:{GetType().Name}")));
            if (after != null) graph.Assert(new Triple(temporalEntityNode, graph.CreateUriNode("time:after"), graph.CreateUriNode($"time:{after}")));
            if (before != null) graph.Assert(new Triple(temporalEntityNode, graph.CreateUriNode("time:before"), graph.CreateUriNode($"time:{before}")));
            if (hasBeginning != null) graph.Assert(new Triple(temporalEntityNode, graph.CreateUriNode("time:hasBeginning"), graph.CreateUriNode($"time:{hasBeginning}")));
            if (hasEnd != null) graph.Assert(new Triple(temporalEntityNode, graph.CreateUriNode("time:hasEnd"), graph.CreateUriNode($"time:{hasEnd}")));
            if (hasXSDDuration != null) graph.Assert(new Triple(temporalEntityNode, graph.CreateUriNode("time:hasXSDDuration"), hasXSDDuration.ToLiteralNode(graph)));
            return temporalEntityNode;
        }

        /// <summary>
        /// Starts the temporal entity.
        /// </summary>
        /// <param name="previous">The temporal entity that occurs before this one.</param>
        public void Start(Instant instant, TemporalEntity previous = null)
        {
            hasBeginning = instant;
            if (previous != null) after = previous;
        }

        /// <summary>
        /// Ends the temporal entity.
        /// </summary>
        /// <param name="next">The temporal entity that occurs after this one.</param>
        public void End(Instant instant, TemporalEntity next = null)
        {
            hasEnd = instant;
            if (next != null) before = next;
            hasXSDDuration = new XSDDuration(hasBeginning.inXSDDateTime, hasEnd.inXSDDateTime);
        }
    }
}