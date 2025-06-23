// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.GraphManagement;
using System;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace Sven.XsdData
{
    /// <summary>
    /// Represents an XSD duration.
    /// </summary>
    public class XSDDuration
    {
        /// <summary>
        /// The duration string.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Initializes a new instance of the XSDDuration class with the specified duration string.
        /// </summary>
        /// <param name="start">The start date and time.</param>
        /// <param name="end">The end date and time.</param>
        public XSDDuration(DateTime start, DateTime end)
        {
            TimeSpan span = end - start;

            int years, months, days;
            CalculateDateDifferences(start, end, out years, out months, out days);

            int hours = span.Hours;
            int minutes = span.Minutes;
            double seconds = span.Seconds + span.Milliseconds / 1000.0;

            Value = BuildDurationString(years, months, days, hours, minutes, seconds);
        }

        /// <summary>
        /// Initializes a new instance of the XSDDuration class with the specified duration string.
        /// </summary>
        /// <param name="start">The start date and time.</param>
        /// <param name="end">The end date and time.</param>
        /// <param name="years">The number of years.</param>
        /// <param name="months">The number of months.</param>
        /// <param name="days">The number of days.</param>
        private void CalculateDateDifferences(DateTime start, DateTime end, out int years, out int months, out int days)
        {
            years = end.Year - start.Year;
            months = end.Month - start.Month;
            days = end.Day - start.Day;

            if (days < 0)
            {
                months--;
                days += DateTime.DaysInMonth(start.Year, start.Month);
            }

            if (months < 0)
            {
                years--;
                months += 12;
            }
        }

        /// <summary>
        /// Builds the duration string.
        /// </summary>
        /// <param name="years">The number of years.</param>
        /// <param name="months">The number of months.</param>
        /// <param name="days">The number of days.</param>
        /// <param name="hours">The number of hours.</param>
        /// <param name="minutes">The number of minutes.</param>
        /// <param name="seconds">The number of seconds.</param>
        /// <returns>The duration string.</returns>
        private string BuildDurationString(int years, int months, int days, int hours, int minutes, double seconds)
        {
            string duration = "P";
            if (years > 0) duration += $"{years}Y";
            if (months > 0) duration += $"{months}M";
            if (days > 0) duration += $"{days}D";
            if (hours > 0 || minutes > 0 || seconds > 0)
            {
                duration += "T";
                if (hours > 0) duration += $"{hours}H";
                if (minutes > 0) duration += $"{minutes}M";
                if (seconds > 0)
                {
                    string formattedSeconds = seconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0');
                    if (formattedSeconds.EndsWith(".")) // Remove trailing dot if any
                    {
                        formattedSeconds = formattedSeconds.TrimEnd('.');
                    }
                    duration += $"{formattedSeconds}S";
                }
            }

            return duration == "P" ? "P0D" : duration;
        }

        /// <summary>
        /// Returns the duration string.
        /// </summary>
        /// <returns>The duration string.</returns>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Converts the duration to a literal node.
        /// </summary>
        /// <param name="factory">The node factory.</param>
        /// <returns>The literal node.</returns>
        public ILiteralNode ToLiteralNode()
        {
            return GraphManager.CreateLiteralNode(Value, new Uri(XmlSpecsHelper.XmlSchemaDataTypeDuration));
        }
    }
}