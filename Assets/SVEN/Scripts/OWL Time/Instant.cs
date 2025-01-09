using System;
using Sven.XsdData;
using VDS.RDF;

namespace Sven.OwlTime
{
    /// <summary>
    /// Represents an instant in time.
    /// </summary>
    public class Instant : TemporalEntity
    {

        /// <summary>
        /// The instant in XSDDateTime format
        /// </summary>
        public readonly DateTime inXSDDateTime;

        /// <summary>
        /// The instant in XSDDate format
        /// </summary>
        private readonly XSDDate inXSDDate;

        /// <summary>
        /// The instant in XSDDateTimeStamp format
        /// </summary>
        private readonly XSDDateTimeStamp inXSDTimeStamp;

        /// <summary>
        /// Initializes a new instance of the Instant class.
        /// </summary>
        /// <returns></returns>
        public Instant() : this(DateTime.Now) { }

        /// <summary>
        /// Initializes a new instance of the Instant class with the specified date and time.
        /// </summary>
        /// <param name="dateTime">The date and time to initialize the instance with.</param>
        public Instant(DateTime dateTime) : base(dateTime)
        {
            inXSDDateTime = dateTime;
            inXSDDate = new XSDDate(dateTime);
            inXSDTimeStamp = new XSDDateTimeStamp(dateTime);
        }

        /// <summary>
        /// Semantizes the temporal entity in the graph.
        /// </summary>
        /// <param name="graph">The graph to semantize the temporal entity.</param>
        public new IUriNode Semantize(IGraph graph)
        {
            IUriNode instantNode = base.Semantize(graph);
            //Interval.SemantizeInside(graph, this);
            if (inXSDDateTime != null) graph.Assert(new Triple(instantNode, graph.CreateUriNode("time:inXSDDateTime"), inXSDDateTime.ToLiteralNode(graph)));
            // seems to be useless in our case
            // if (inXSDDate != null) graph.Assert(new Triple(temporalEntityNode, graph.CreateUriNode("time:inXSDDate"), inXSDDate.ToLiteralNode(graph)));
            // if (inXSDTimeStamp != null) graph.Assert(new Triple(temporalEntityNode, graph.CreateUriNode("time:inXSDTimeStamp"), inXSDTimeStamp.ToLiteralNode(graph)));
            return instantNode;
        }
    }
}