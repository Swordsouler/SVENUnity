using System;
using System.Collections.Generic;

namespace OWLTime
{
    /// <summary>
    /// Represents a time interval with a specific instant inside it.
    /// </summary>
    public class Interval : TemporalEntity
    {
        /// <summary>
        /// The instant inside the interval.
        /// </summary>
        /// <returns></returns>
        public List<Instant> inside = new();

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
    }
}