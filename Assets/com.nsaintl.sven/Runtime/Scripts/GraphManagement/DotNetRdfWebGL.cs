
#if !(!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Collections.Generic;
using System.IO;
using Sven.Utils;
using UnityEngine;
using UnityEngine.Networking;
using VDS.RDF.Parsing;
#endif
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Query;

namespace Sven.GraphManagement
{
    public static class DotNetRdfWebGL
    {
        public static async Task<SparqlResultSet> QueryWebGLWithResultSetAsync(this SparqlQueryClient client, string query)
        {
#if !(!UNITY_WEBGL || UNITY_EDITOR)
            TaskCompletionSource<SparqlResultSet> tcs = new();
            UnityMainThreadDispatcher.Instance.Enqueue(async () =>
            {
                try
                {
                    using UnityWebRequest request = UnityWebRequest.Post(client.EndpointUri, new Dictionary<string, string>
                    {
                        { "query", query },
                        { "format", "application/sparql-results+json" }
                    });

                    request.SetRequestHeader("Accept", "application/sparql-results+json");

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string jsonResponse = request.downloadHandler.text;

                        SparqlResultSet resultSet = new();
                        SparqlJsonParser parser = new();
                        using StringReader reader = new(jsonResponse);
                        parser.Load(resultSet, reader);

                        tcs.SetResult(resultSet);
                    }
                    else
                    {
                        Debug.LogError($"Erreur lors de la requête SPARQL : {request.error}");
                        tcs.SetException(new Exception($"SPARQL query failed: {request.error}"));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception dans QueryWebGLWithResultSetAsync : {ex}");
                    tcs.SetException(ex);
                }
            });

            SparqlResultSet resultSet = await tcs.Task;

            //foreach (SparqlResult result in resultSet.Cast<SparqlResult>()) Debug.Log($"Result: {result.ToString()}");

            return resultSet;
#else
            return await client.QueryWithResultSetAsync(query);
#endif
        }

        // returns a graph from the query result (CONSTRUCT query)
        public static async Task<IGraph> QueryWebGLWithResultGraphAsync(this SparqlQueryClient client, string query)
        {
#if !(!UNITY_WEBGL || UNITY_EDITOR)
            TaskCompletionSource<IGraph> tcs = new();
            UnityMainThreadDispatcher.Instance.Enqueue(async () =>
            {
                try
                {
                    using UnityWebRequest request = UnityWebRequest.Post(client.EndpointUri, new Dictionary<string, string>
                    {
                        { "query", query },
                        { "format","application/rdf+xml" }
                    });

                    request.SetRequestHeader("Accept", "application/rdf+xml");

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = request.downloadHandler.text;

                        IGraph resultGraph = new Graph();
                        IRdfReader rdfParser = MimeTypesHelper.GetParser("application/rdf+xml");
                        using StringReader reader = new(responseText);
                        rdfParser.Load(resultGraph, reader);


                        tcs.SetResult(resultGraph);
                    }
                    else
                    {
                        Debug.LogError($"Erreur lors de la requête SPARQL : {request.error}");
                        tcs.SetException(new Exception($"SPARQL query failed: {request.error}"));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception dans QueryWebGLWithResultSetAsync : {ex}");
                    tcs.SetException(ex);
                }
            });

            IGraph resultGraph = await tcs.Task;

            //foreach (SparqlResult result in resultSet.Cast<SparqlResult>()) Debug.Log($"Result: {result.ToString()}");

            return resultGraph;
#else
            return await client.QueryWithResultGraphAsync(query);
#endif
        }
    }
}