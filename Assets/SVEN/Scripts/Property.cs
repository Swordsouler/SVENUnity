using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SVEN
{
    /// <summary>
    /// Observes changes in a single property and triggers multiple callbacks when the property changes.
    /// </summary>
    public class Property
    {
        /// <summary>
        /// Unique identifier for the resource.
        /// </summary>
        private readonly string resourceID = Guid.NewGuid().ToString();
        /// <summary>
        /// Gets the resource identifier for the property.
        /// </summary>
        /// <returns>Resource identifier.</returns>
        public string ResourceID()
        {
            return resourceID;
        }

        /// <summary>
        /// Gets the resource for the property.
        /// </summary>
        /// <returns>Resource.</returns>
        public string Resource()
        {
            return ResourceID();
        }

        /// <summary>
        /// The name of the property to observe.
        /// </summary>
        private readonly string name;

        /// <summary>
        /// Gets the name of the property to observe.
        /// </summary>
        public string Name => name;

        private class ObservedProperty
        {
            /// <summary>
            /// A list of functions that return the values of the properties to observe.
            /// </summary>
            public Func<object> Getter;

            /// <summary>
            /// The callback to invoke when any of the observed properties change.
            /// </summary>
            public List<Action> Callbacks = new();

            /// <summary>
            /// The last known values of the observed properties.
            /// </summary>
            public object LastValue;
        }

        private readonly ObservedProperty observedProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyObserver"/> class.
        /// </summary>
        /// <param name="getter">A function that returns the value of the property to observe.</param>
        public Property(string name, Func<object> getter)
        {
            this.name = name;
            observedProperty = new ObservedProperty
            {
                Getter = getter,
                LastValue = getter()
            };
        }

        /// <summary>
        /// Adds a new callback to invoke when the observed property changes.
        /// </summary>
        /// <param name="callback">The callback to add.</param>
        public void AddCallback(Action callback)
        {
            observedProperty.Callbacks.Add(callback);
        }

        // <summary>
        /// Removes a callback from the list of callbacks to invoke when the observed property changes.
        /// </summary>
        public void RemoveCallback(Action callback)
        {
            observedProperty.Callbacks.Remove(callback);
        }

        /// <summary>
        /// Removes all callbacks from the list of callbacks to invoke when the observed property changes.
        /// </summary>  
        public void RemoveAllCallbacks()
        {
            observedProperty.Callbacks.Clear();
        }

        /// <summary>
        /// Checks if the observed property has changed and invokes the callbacks if it has.
        /// </summary>
        public void CheckForChanges()
        {
            object currentValue = observedProperty.Getter();
            //UnityEngine.Debug.Log("Current value: " + currentValue + " Last value: " + observedProperty.LastValue);
            if (!Equals(currentValue, LastValue))
            {
                observedProperty.LastValue = currentValue;
                foreach (Action callback in observedProperty.Callbacks)
                {
                    callback?.Invoke();
                }
            }
        }

        /// <summary>
        /// Gets the last known value of the observed property.
        /// </summary>
        public object LastValue => observedProperty.LastValue;
    }
}