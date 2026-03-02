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
    /// Represents the pointer in the scene.
    /// </summary>
    public class Pointer : Interactor
    {
        /// <summary>
        /// The maximum distance for the pointer.
        /// </summary>
        [field: SerializeField]
        public int PointerIndex { get; set; } = 0;
        [field: SerializeField]
        public float PointerDistance { get; set; } = 20f;
        [field: SerializeField]
        public float PointerConeAngle { get; set; } = 0f;
        [field: SerializeField]
        public Vector3 PointerPosition { get; set; } = Vector3.zero;
        [field: SerializeField]
        public Vector3 PointerDirection { get; set; } = Vector3.forward;
        [field: SerializeField]
        public Vector3 PointerHitPosition { get; set; } = Vector3.zero;
        [field: SerializeField]
        public float PointerHitDistance { get; set; } = 0f;

        protected override IEnumerator CheckInteractor(float i)
        {
            while (true)
            {
                PointerPosition = transform.position;
                PointerDirection = transform.forward;
                float visionDistance = PointerDistance;

                Ray ray = new(PointerPosition, PointerDirection);
                RaycastHit[] hits;

                if (PointerConeAngle > 0f)
                {
                    hits = GetConeCastHits(PointerPosition, PointerDirection, PointerDistance, PointerConeAngle);
                }
                else
                {
                    hits = Physics.RaycastAll(ray, visionDistance);
                }

                // Détermine le point le plus proche touché par le rayon ; si aucun hit, prend le point à la distance maximale du pointer
                if (hits != null && hits.Length > 0)
                {
                    float minDist = float.MaxValue;
                    Vector3 closestPoint = PointerPosition + PointerDirection.normalized * visionDistance;
                    for (int k = 0; k < hits.Length; k++)
                    {
                        RaycastHit h = hits[k];
                        if (h.distance < minDist)
                        {
                            minDist = h.distance;
                            closestPoint = h.point;
                        }
                    }
                    PointerHitPosition = closestPoint;
                    PointerHitDistance = minDist;
                }
                else
                {
                    PointerHitPosition = PointerPosition + PointerDirection.normalized * visionDistance;
                    PointerHitDistance = visionDistance;
                }

                HashSet<SemantizationCore> newVisibleObjects = new();

                for (int j = 0; j < hits.Length; j++)
                {
                    RaycastHit hit = hits[j];
                    Collider collider = hit.collider;
                    if (collider.TryGetComponent(out SemantizationCore semantizationCore))
                    {
                        newVisibleObjects.Add(semantizationCore);
                        if (!currentInteractedObjects.Contains(semantizationCore))
                        {
                            // Object enters the field of view, create interval for interaction and semantize the action
                            string dictionaryKey = $"{_semantizationCore.GetUUID()}-{semantizationCore.GetUUID()}";
                            // call start interval semantization of collisionevent
                            if (!_collisionEvents.ContainsKey(dictionaryKey))
                            {
                                if (SvenSettings.Debug) Debug.Log("Object " + semantizationCore.name + " enters the pointer range.");
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
                        // Object exits the field of view, close interval for interaction and semantize the action
                        // call end interval semantization of collisionevent
                        string dictionaryKey = $"{_semantizationCore.GetUUID()}-{obj.GetUUID()}";
                        if (_collisionEvents.TryGetValue(dictionaryKey, out CollisionEvent collisionEvent))
                        {
                            if (SvenSettings.Debug) Debug.Log("Object " + obj.name + " exits the pointer range.");
                            collisionEvent.End(GraphManager.CurrentInstant);
                            collisionEvent.Semanticize();
                            _collisionEvents.Remove(dictionaryKey);
                        }
                    }
                }
                // sort the hashset by distance to the pointer
                List<SemantizationCore> sortedVisibleObjects = new(newVisibleObjects);
                sortedVisibleObjects.Sort((a, b) => Vector3.Distance(a.transform.position, PointerPosition).CompareTo(Vector3.Distance(b.transform.position, PointerPosition)));
                // Update the list of currently interacted objects
                currentInteractedObjects.Clear();
                currentInteractedObjects.UnionWith(sortedVisibleObjects);

                yield return new WaitForSeconds(i);
            }
        }

        private RaycastHit[] GetConeCastHits(Vector3 origin, Vector3 direction, float distance, float coneAngleDegrees)
        {
            Dictionary<Collider, RaycastHit> uniqueHits = new();
            float coneAngleRad = coneAngleDegrees * Mathf.Deg2Rad;

            // Calcule la sphère englobante du cône
            float maxConeRadius = distance * Mathf.Tan(coneAngleRad);
            float sphereRadius = Mathf.Sqrt(distance * distance + maxConeRadius * maxConeRadius);

            // Obtient tous les colliders potentiels dans la sphère
            Collider[] allColliders = Physics.OverlapSphere(origin + direction.normalized * (distance * 0.5f), sphereRadius);

            // Valide chaque collider et crée un hit
            foreach (Collider collider in allColliders)
            {
                // Récupère le point le plus proche du collider par rapport à l'origine
                Vector3 closestPoint = collider.ClosestPoint(origin);
                Vector3 pointToOrigin = closestPoint - origin;
                float distToPoint = pointToOrigin.magnitude;

                // Filtre par distance et angle du cône
                if (distToPoint <= distance && IsPointInCone(origin, direction, closestPoint, coneAngleRad, distance))
                {
                    // Crée un raycast depuis l'origine vers le collider pour obtenir le vrai hit
                    Ray rayToCollider = new(origin, pointToOrigin.normalized);
                    if (collider.Raycast(rayToCollider, out RaycastHit hit, distance))
                    {
                        if (!uniqueHits.ContainsKey(collider))
                            uniqueHits.Add(collider, hit);
                    }
                }
            }

            return uniqueHits.Values.ToArray();
        }

        private bool IsPointInCone(Vector3 coneOrigin, Vector3 coneDirection, Vector3 point, float coneAngleRad, float maxDistance)
        {
            Vector3 pointVector = point - coneOrigin;
            float distance = pointVector.magnitude;

            // Vérifie la distance maximale
            if (distance > maxDistance)
                return false;

            // Évite la division par zéro
            if (distance < 0.01f)
                return true;

            // Calcule l'angle entre le point et la direction du cône
            float angle = Vector3.Angle(coneDirection, pointVector) * Mathf.Deg2Rad;

            // Le point est dans le cône si l'angle est <= angle du cône
            return angle <= coneAngleRad;
        }

        protected new void OnDrawGizmos()
        {
            if (!_debug) return;
            Gizmos.color = SvenSettings.PointerDebugColor;
            base.OnDrawGizmos();

            Vector3 direction = transform.forward;
            Vector3 origin = transform.position;
            Vector3 destination = origin + direction * PointerDistance;

            if (PointerConeAngle > 0f)
            {
                DrawCone(origin, direction, PointerDistance, PointerConeAngle, 12);
            }
            else
            {
                Gizmos.DrawLine(origin, destination);
            }
        }

        private void DrawCone(Vector3 origin, Vector3 direction, float distance, float coneAngleDegrees, int segments)
        {
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (right.magnitude < 0.01f)
                right = Vector3.Cross(direction, Vector3.right).normalized;

            Vector3 up = Vector3.Cross(right, direction).normalized;
            float coneRad = coneAngleDegrees * Mathf.Deg2Rad;

            // Dessine des lignes d'arête du cône
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * 360f * Mathf.Deg2Rad;
                Vector3 rayDirection = direction.normalized +
                                       (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * Mathf.Tan(coneRad);
                rayDirection.Normalize();

                Vector3 endPoint = origin + rayDirection * distance;
                Gizmos.DrawLine(origin, endPoint);
            }

            // Dessine des cercles transversaux pour mieux visualiser
            for (int slice = 1; slice < 4; slice++)
            {
                float sliceDistance = distance * (slice / 4f);
                float sliceRadius = sliceDistance * Mathf.Tan(coneRad);

                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (i / (float)segments) * 360f * Mathf.Deg2Rad;
                    float angle2 = ((i + 1) / (float)segments) * 360f * Mathf.Deg2Rad;

                    Vector3 point1 = origin + direction.normalized * sliceDistance +
                                     (Mathf.Cos(angle1) * right + Mathf.Sin(angle1) * up) * sliceRadius;
                    Vector3 point2 = origin + direction.normalized * sliceDistance +
                                     (Mathf.Cos(angle2) * right + Mathf.Sin(angle2) * up) * sliceRadius;

                    Gizmos.DrawLine(point1, point2);
                }
            }
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
    sven:Pointer rdfs:label ?label .
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
    sven:Pointer sven:deicticWord ?label .
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