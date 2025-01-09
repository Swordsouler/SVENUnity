using System;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace Sven.XsdData
{
    /// <summary>
    /// Represents a date in the XSD format
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts the date to a literal node.
        /// </summary>
        /// <param name="factory">The node factory.</param>
        /// <returns>The literal node.</returns>
        public static ILiteralNode ToLiteralNode(this DateTime dateTime, INodeFactory factory)
        {
            return factory.CreateLiteralNode(dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"), new Uri(XmlSpecsHelper.XmlSchemaDataTypeDateTime));
        }
    }
}