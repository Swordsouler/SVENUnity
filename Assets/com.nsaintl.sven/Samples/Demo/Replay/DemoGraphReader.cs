// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Sven.Content;
using Sven.GraphManagement;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace Sven.Demo
{
    public class DemoGraphReader : GraphReader
    {
        public new void Awake()
        {
            readingMode = GraphStorageMode.Remote;
            graphName = DemoGraphConfig.graphName;
            endpoint = DemoGraphConfig.EndpointUri.ToString();
            MapppedComponents.AddComponentDescription(typeof(DemoSprayController), new("Spray",
                new List<Delegate>
                {
                    (Func<DemoSprayController, MapppedComponents.PropertyDescription>)(spray => new MapppedComponents.PropertyDescription("enabled", () => spray.enabled, value => spray.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Pumpkin), new("PumpkinComponent",
                new List<Delegate>
                {
                    (Func<Pumpkin, MapppedComponents.PropertyDescription>)(pumpkin => new MapppedComponents.PropertyDescription("enabled", () => pumpkin.enabled, value => pumpkin.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Apple), new("AppleComponent",
                new List<Delegate>
                {
                    (Func<Apple, MapppedComponents.PropertyDescription>)(apple => new MapppedComponents.PropertyDescription("enabled", () => apple.enabled, value => apple.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Banana), new("BananaComponent",
                new List<Delegate>
                {
                    (Func<Banana, MapppedComponents.PropertyDescription>)(banana => new MapppedComponents.PropertyDescription("enabled", () => banana.enabled, value => banana.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Carrot), new("CarrotComponent",
                new List<Delegate>
                {
                    (Func<Carrot, MapppedComponents.PropertyDescription>)(carrot => new MapppedComponents.PropertyDescription("enabled", () => carrot.enabled, value => carrot.enabled = value.ToString() == "true", 1)),
                }, 1));
            base.Awake();
        }

        public async Task<string> GetTTL()
        {
            string query = $@"PREFIX time: <http://www.w3.org/2006/time#>
PREFIX sven: <https://sven.lisn.upsaclay.fr/entity/>
PREFIX geo: <http://www.opengis.net/ont/geosparql#>

CONSTRUCT {{
    ?s ?p ?o .
}}
FROM <{graph.BaseUri.AbsoluteUri}{Uri.EscapeDataString(graphName)}>
WHERE {{
    ?s ?p ?o .
}}";

            Uri endpointUri = new(_loadedEndpoint);
            HttpClient httpClient = new();

            var byteArray = Encoding.ASCII.GetBytes($"admin:sven-iswc");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            SparqlQueryClient sparqlQueryClient = new(httpClient, endpointUri);
#if UNITY_WEBGL && !UNITY_EDITOR
            string ttlContent = await sparqlQueryClient.QueryWebGLWithResultTTLAsync(query);
#else
            IGraph resultGraph = await sparqlQueryClient.QueryWithResultGraphAsync(query);
            foreach (GraphNamespace graphNamespace in ontologyDescription.Namespaces)
            {
                if (!resultGraph.NamespaceMap.HasNamespace(graphNamespace.Name))
                    resultGraph.NamespaceMap.AddNamespace(graphNamespace.Name, new Uri(graphNamespace.Uri));
            }

            string ttlContent = DecodeGraph(resultGraph);
#endif
            return ttlContent;
        }
    }
}
