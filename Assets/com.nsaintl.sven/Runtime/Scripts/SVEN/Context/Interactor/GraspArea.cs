// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.Content;
using Sven.GraphManagement;
using Sven.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace Sven.Context
{
    /// <summary>
    /// Represents the grasp area in the scene.
    /// </summary>
    public class GraspArea : Interactor
    {
        /// <summary>
        /// The radius of the sphere.
        /// </summary>
        [field: SerializeField]
        public float GraspDistance { get; set; } = 10f;

        protected override IEnumerator CheckInteractor(float i)
        {
            while (true)
            {
                Vector3 sphereCenter = transform.position;
                float radius = GraspDistance;

                Collider[] colliders = Physics.OverlapSphere(sphereCenter, radius);
                HashSet<SemantizationCore> newVisibleObjects = new();

                for (int j = 0; j < colliders.Length; j++)
                {
                    Collider collider = colliders[j];
                    if (collider.TryGetComponent(out SemantizationCore semantizationCore))
                    {
                        newVisibleObjects.Add(semantizationCore);
                        if (!currentInteractedObjects.Contains(semantizationCore))
                        {
                            // Object enters the sphere area, create interval for interaction and semantize the action
                            string dictionaryKey = $"{_semantizationCore.GetUUID()}-{semantizationCore.GetUUID()}";
                            // call start interval semantization of collisionevent
                            if (!_collisionEvents.ContainsKey(dictionaryKey))
                            {
                                if (SvenSettings.Debug) Debug.Log("Object " + semantizationCore.name + " enters the grasp area.");
                                CollisionEvent collisionEvent = new(_semantizationCore, semantizationCore);
                                collisionEvent.Start(GraphManager.CurrentInstant);
                                collisionEvent.Semanticize();
                                _collisionEvents.Add(dictionaryKey, collisionEvent);
                            }
                        }
                    }
                }

                // Detect objects that are no longer visible
                foreach (SemantizationCore obj in currentInteractedObjects)
                {
                    if (!newVisibleObjects.Contains(obj))
                    {
                        // Object exits the sphere area, close interval for interaction and semantize the action
                        // call end interval semantization of collisionevent
                        string dictionaryKey = $"{_semantizationCore.GetUUID()}-{obj.GetUUID()}";
                        if (_collisionEvents.TryGetValue(dictionaryKey, out CollisionEvent collisionEvent))
                        {
                            if (SvenSettings.Debug) Debug.Log("Object " + obj.name + " exits the grasp area.");
                            collisionEvent.End(GraphManager.CurrentInstant);
                            collisionEvent.Semanticize();
                            _collisionEvents.Remove(dictionaryKey);
                        }
                    }
                }

                // Update the list of currently visible objects
                currentInteractedObjects = newVisibleObjects;
                yield return new WaitForSeconds(i);
            }
        }

        protected new void OnDrawGizmos()
        {
            if (!_debug) return;
            Gizmos.color = SvenSettings.GraspAreaDebugColor;
            base.OnDrawGizmos();

            Vector3 center = transform.position;
            Gizmos.DrawWireSphere(center, GraspDistance);
        }

        private static List<string> _availableNames;
        private static List<string> _availableDeictics;
        private static string _cachedLocale;

        public static async Task<List<string>> GetAvailableNamesAsync(string locale)
        {
            if (_availableNames == null || _cachedLocale != locale)
            {
                _availableNames = await GetAllAvailableNames(locale);
                _cachedLocale = locale;
            }
            return _availableNames;
        }

        public static async Task<List<string>> GetAllAvailableNames(string locale)
        {
            // load a graph with colors from resources
            Graph graph = new();
            // load ontology like GraphManager
            Dictionary<string, string> ontologies = await SvenSettings.GetOntologiesAsync();
            TurtleParser turtleParser = new();
            foreach (KeyValuePair<string, string> ontology in ontologies)
            {
                turtleParser.Load(graph, ontology.Value);
            }

            string query = $@"
PREFIX sven: <https://sven.lisn.upsaclay.fr/ontology#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>

SELECT ?label
WHERE {{
    sven:GraspAreaObject rdfs:label ?label .
    FILTER(langMatches(lang(?label), ""{locale}""))
}}";

            if (graph.ExecuteQuery(query) is SparqlResultSet results)
            {
                return results.Select(result => (result["label"] as ILiteralNode)?.Value).Where(label => label != null).ToList();
            }

            return new List<string>();
        }

        public static async Task<List<string>> GetAvailableDeicticsAsync(string locale)
        {
            if (_availableDeictics == null || _cachedLocale != locale)
            {
                _availableDeictics = await GetAllAvailableDeictics(locale);
                _cachedLocale = locale;
            }
            return _availableDeictics;
        }

        public static async Task<List<string>> GetAllAvailableDeictics(string locale)
        {
            // load a graph with colors from resources
            Graph graph = new();
            // load ontology like GraphManager
            Dictionary<string, string> ontologies = await SvenSettings.GetOntologiesAsync();
            TurtleParser turtleParser = new();
            foreach (KeyValuePair<string, string> ontology in ontologies)
            {
                turtleParser.Load(graph, ontology.Value);
            }
            string query = $@"
PREFIX sven: <https://sven.lisn.upsaclay.fr/ontology#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
SELECT ?label
WHERE {{
    sven:GraspAreaObject sven:deicticWord ?label .
    FILTER(langMatches(lang(?label), ""{locale}""))
}}";
            if (graph.ExecuteQuery(query) is SparqlResultSet results)
            {
                return results.Select(result => (result["label"] as ILiteralNode)?.Value).Where(label => label != null).ToList();
            }
            return new List<string>();
        }
    }
}