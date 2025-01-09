using System;
using VDS.RDF;

namespace Sven.OwlTime
{
    /// <summary>
    /// Represents a time interval with a specific instant inside it.
    /// </summary>
    public class Interval : TemporalEntity
    {
        /// <summary>
        /// Initializes a new instance of the Interval class.
        /// </summary>
        public Interval() : base("time:", Guid.NewGuid().ToString()) { }

        /// <summary>
        /// Initializes a new instance of the Interval class with the specified UUID.
        /// </summary>
        /// <param name="UUID">The UUID to initialize the instance with.</param>
        public Interval(string UUID) : base("time:", UUID) { }

        public Interval(string prefix, string UUID) : base(prefix, UUID) { }

        /// <summary>
        /// Initializes a new instance of the Interval class.
        /// </summary>
        public Interval(DateTime dateTime) : this() { }

        /// <summary>
        /// Starts the interval.
        /// </summary>
        /// <param name="previous">The interval that occurs before this one.</param>
        public new void Start(Instant instant, TemporalEntity previous = null)
        {
            intervals.Add(this);
            base.Start(instant, previous);
        }

        /// <summary>
        /// Ends the interval.
        /// </summary>
        /// <param name="next">The interval that occurs after this one.</param>
        public new void End(Instant instant, TemporalEntity next = null)
        {
            intervals.Remove(this);
            base.End(instant, next);
        }

        /// <summary>
        /// Semantizes the instant inside the interval.
        /// </summary>
        /// <param name="graph">The graph to semantize the interval.</param>
        /// <param name="instant">The instant to semantize inside the interval.</param>
        public static void SemantizeInside(IGraph graph, Instant instant)
        {
            IUriNode instantNode = instant.GetUriNode(graph);

            foreach (Interval interval in intervals)
            {
                graph.Assert(new Triple(interval.GetUriNode(graph), graph.CreateUriNode("time:inside"), instantNode));
            }
        }
    }
}