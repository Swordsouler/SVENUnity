// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Sven.GraphManagement;
using Sven.OwlTime;
using Sven.Utils;
using UnityEngine;
using VDS.RDF;

namespace Sven.Content
{
    /// <summary>
    /// Core component to semantize content.
    /// </summary>
    [DisallowMultipleComponent,
    AddComponentMenu("Semantic/Semantization Core")]
    public class SemantizationCore : MonoBehaviour
    {
        /// <summary>
        /// Components to semantize at initialization. WARNING: Do not use this list for algorithmic purposes (it's just for the Unity Editor).
        /// </summary>
        [HideInInspector]
        public List<SemanticComponent> componentsToSemantize = new();

        /// <summary>
        /// Properties of the each Component to semantize.
        /// </summary>
        private readonly Dictionary<Component, SemanticComponent> componentsProperties = new();

        /// <summary>
        /// The graph buffer to semantize the GameObject.
        /// </summary>
        [SerializeField]
        private GraphBuffer _graphBuffer;
        /// <summary>
        /// The graph buffer to semantize the GameObject.
        /// </summary>
        public GraphBuffer GraphBuffer => _graphBuffer;

        /// <summary>
        /// Coroutine to check for changes in the properties of the GameObject.
        /// </summary>
        private Coroutine _checkForChangesCoroutine;

        /// <summary>
        /// Start is called before the first frame update.
        /// </summary>
        private void Start()
        {
            if (_graphBuffer == null) _graphBuffer = GraphManager.Get("sven");
            if (_graphBuffer == null) return;
            Component component = GetComponent<Component>();
            componentsToSemantize.RemoveAll(c => c == null || c.Component == null || !component.gameObject.Equals(c.Component.gameObject));
            Initialize();
            _checkForChangesCoroutine = StartCoroutine(LoopCheckForChanges(1.0f / _graphBuffer.instantPerSecond));
        }

        /// <summary>
        /// Initializes the semantization of the GameObject, his components and setup the properties observers.
        /// </summary>
        private void Initialize()
        {
            try
            {
                // Semantize the GameObject attached to his properties and components
                componentsProperties.Add(this, new SemanticComponent
                {
                    Component = this,
                    ProcessingMode = SemanticProcessingMode.Dynamic,
                    Properties = SemanticObserve(_graphBuffer)
                });

                // foreach component in the GameObject, semantize the component and his properties
                foreach (var component in componentsToSemantize)
                    componentsProperties.Add(component.Component, new SemanticComponent
                    {
                        Component = component.Component,
                        ProcessingMode = component.ProcessingMode,
                        Properties = component.Component.SemanticObserve(_graphBuffer)
                    });

            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Adds a component to the semantization process on the fly. WARNING: When adding components, it will be semantized until his end of life.
        /// </summary>
        /// <param name="component">The component to add to the semantization process.</param>
        public void AddSemanticComponent(Component component, SemanticProcessingMode mode)
        {
            if (componentsToSemantize.Find(c => c.Component == component) != null)
            {
                Debug.LogWarning("Component " + component.GetType().Name + " is already being semantized.");
                return;
            }

            componentsToSemantize.Add(new SemanticComponent { Component = component, ProcessingMode = mode });
            List<Property> properties = component.SemanticObserve(_graphBuffer);
            componentsProperties.Add(component, new SemanticComponent
            {
                Component = component,
                ProcessingMode = mode,
                Properties = properties
            });
        }

        /// <summary>
        /// Overrides the default SemanticObserve method to focus on the GameObject semantization.
        /// </summary>
        /// <param name="graphBuffer">The graph buffer to semantize the GameObject.</param>
        public List<Property> SemanticObserve(GraphBuffer graphBuffer)
        {
            List<Property> properties = new()
            {
                new Property("name", () => gameObject.name),
                new Property("active", () => gameObject.activeSelf),
                new Property("tag", () => gameObject.tag),
                new Property("layer", () => LayerMask.LayerToName(gameObject.layer))
            };

            IGraph graph = graphBuffer.Graph;

            IUriNode gameObjectNode = graph.CreateUriNode("sven:" + this.GetUUID());

            graph.Assert(new Triple(gameObjectNode, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("sven:VirtualObject")));
            graph.Assert(new Triple(gameObjectNode, graph.CreateUriNode("rdfs:label"), graph.CreateLiteralNode(name)));
            foreach (Property property in properties)
            {
                property.SemanticObserve(graphBuffer, this);
                if (SvenHelper.Debug)
                    Debug.Log("Observing property (" + name + ")." + GetType().Name + "." + property.Name);
            }

            Interval interval = this.GetInterval();
            interval.Start(graphBuffer.CurrentInstant);
            IUriNode intervalNode = interval.Semantize(graph);
            graph.Assert(new Triple(gameObjectNode, graph.CreateUriNode("time:hasTemporalExtent"), intervalNode));

            return properties;
        }

        #region Properties Observers

        /// <summary>
        /// Checks if the observed properties have changed and invokes the callbacks if they have.
        /// </summary>
        private void CheckForChanges()
        {
            List<Component> toRemove = new();
            foreach (KeyValuePair<Component, SemanticComponent> componentProperties in componentsProperties)
            {
                if (componentProperties.Value.IsSemantized && componentProperties.Value.ProcessingMode == SemanticProcessingMode.Static) continue;
                componentProperties.Value.IsSemantized = true;

                try
                {
                    foreach (Property property in componentProperties.Value.Properties)
                        property.CheckForChanges();
                }
                catch
                {
                    if (SvenHelper.Debug) Debug.LogWarning("Component " + componentProperties.Key.GetType().Name + " has been destroyed. Removing from semantization.");
                    toRemove.Add(componentProperties.Key);
                }
            }

            foreach (Component component in toRemove)
            {
                foreach (Property property in componentsProperties[component].Properties)
                    property.Destroy();

                Interval interval = component.GetInterval();
                interval.End(_graphBuffer.CurrentInstant);
                interval.Semantize(_graphBuffer.Graph);
                component.DestroyUUID();

                componentsProperties.Remove(component);
            }
        }

        /// <summary>
        /// Coroutine to check for changes in the properties of the GameObject.
        /// </summary>
        private IEnumerator LoopCheckForChanges(float interval)
        {
            while (componentsProperties.Count > 0)
            {
                CheckForChanges();
                yield return new WaitForSeconds(interval);
            }
        }

        /// <summary>
        /// OnDisable is called when the behaviour becomes disabled or inactive.
        /// </summary>
        private void OnDisable()
        {
            CheckForChanges();
            if (_checkForChangesCoroutine != null) StopCoroutine(_checkForChangesCoroutine);
        }

        /// <summary>
        /// OnEnable is called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            if (_graphBuffer == null) return;
            if (_checkForChangesCoroutine != null) StopCoroutine(_checkForChangesCoroutine);
            StartCoroutine(LoopCheckForChanges(1.0f / _graphBuffer.instantPerSecond));
        }

        /// <summary>
        /// This function is called when the MonoBehaviour will be destroyed.
        /// </summary>
        public void OnDestroy()
        {
            foreach (KeyValuePair<Component, SemanticComponent> componentProperties in componentsProperties)
            {
                foreach (Property property in componentProperties.Value.Properties)
                    property.Destroy();

                Interval interval = componentProperties.Key.GetInterval();
                interval.End(_graphBuffer.CurrentInstant);
                interval.Semantize(_graphBuffer.Graph);
                componentProperties.Key.DestroyUUID();
            }
        }

        #endregion
    }
}