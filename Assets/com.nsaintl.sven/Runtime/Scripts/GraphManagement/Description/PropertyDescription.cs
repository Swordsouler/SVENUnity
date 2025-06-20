// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sven.GraphManagement.Description
{
    /// <summary>
    /// Property description.
    /// </summary>
    public class PropertyDescription
    {
        /// <summary>
        /// UUID of the Property.
        /// </summary>
        public string UUID { get; set; }

        /// <summary>
        /// Name of the property.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the property.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Values of the property.
        /// </summary>
        public Dictionary<string, object> Values { get; set; }

        /// <summary>
        /// Value of the property.
        /// </summary>
        private object _value = null;
        /// <summary>
        /// Value of the property.
        /// </summary>
        public object Value
        {
            get
            {
                _value ??= GenerateValue();
                return _value;
            }
            private set => _value = value;
        }

        /// <summary>
        /// Generate the value of the property.
        /// </summary>
        /// <returns>Value of the property.</returns>
        public object GenerateValue()
        {
            if (Type == typeof(object))
            {
                return Values["value"];
            }


            try
            {
                // try to create an instance of the property directly with the parameters and constructor
                ConstructorInfo constructor = Type.GetConstructors()
                                      .OrderByDescending(c => c.GetParameters().Length)
                                      .FirstOrDefault();

                ParameterInfo[] parameterInfos = constructor.GetParameters();
                object[] orderedParameters = new object[parameterInfos.Length];
                object[] parameters = Values.Select(x => x.Value).ToArray();

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    var paramInfo = parameterInfos[i];
                    var value = Values.FirstOrDefault(v => v.Key == paramInfo.Name).Value;
                    orderedParameters[i] = value ?? throw new InvalidOperationException($"No value provided for parameter '{paramInfo.Name}'.");
                }

                return Activator.CreateInstance(Type, orderedParameters);
            }
            catch (MissingMethodException)
            {
                // if the constructor is not found, try to create a default instance and set the properties
                object instance = Activator.CreateInstance(Type);
                foreach (KeyValuePair<string, object> kvp in Values)
                {
                    PropertyInfo property = Type.GetProperty(kvp.Key);
                    property?.SetValue(instance, Convert.ChangeType(kvp.Value, property.PropertyType));
                    if (property != null) continue;

                    //try in fields
                    FieldInfo field = Type.GetField(kvp.Key);
                    field?.SetValue(instance, Convert.ChangeType(kvp.Value, field.FieldType));
                }
                return instance;
            }
        }

        public PropertyDescription(string uuid, string name, Type type)
        {
            UUID = uuid;
            Name = name;
            Type = type;
            Values = new();
        }

        public PropertyDescription(string uuid, string name, Type type, Dictionary<string, object> values)
        {
            UUID = uuid;
            Name = name;
            Type = type;
            Values = values;
        }

        /// <summary>
        /// ToString method.
        /// </summary>
        /// <returns>String representation of the property description.</returns>
        public override string ToString()
        {
            int maxValueSize = 50;
            // x.Value has a limited size of 50 characters
            return string.Join(", ", Values.Select(x => $"{x.Key}: \"{(x.Value.ToString().Length > maxValueSize ? x.Value.ToString()[..maxValueSize] + "..." : x.Value.ToString())}\""));
        }
    }
}
