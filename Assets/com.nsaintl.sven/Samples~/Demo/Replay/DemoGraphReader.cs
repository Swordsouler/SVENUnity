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
            graphName = DemoManager.graphName;
            endpoint = DemoManager.EndpointUri.ToString();
            MapppedComponents.AddComponentDescription(typeof(DemoSprayController), new("Spray",
                new List<Delegate>
                {
                    (Func<DemoSprayController, MapppedComponents.PropertyDescription>)(spray => new MapppedComponents.PropertyDescription("enabled", () => spray.enabled, value => spray.enabled = value.ToString() == "true", 1)),
                }, 1));
            base.Awake();
        }

        public async Task<string> GetTTL()
        {
            string query = $@"PREFIX time: <http://www.w3.org/2006/time#>
PREFIX sven: <http://www.sven.fr/>
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
