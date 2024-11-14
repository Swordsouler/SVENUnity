using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SVEN
{
    /// <summary>
    /// Provides static methods for executing SPARQL queries and updates against a SPARQL endpoint.
    /// </summary>
    public static class SparqlRequest
    {
        /// <summary>
        /// The HttpClient used for making HTTP requests.
        /// </summary>
        private static readonly HttpClient HttpClient = new();

        /// <summary>
        /// Executes a SPARQL query against the specified endpoint URL.
        /// </summary>
        /// <param name="endpointUrl">The URL of the SPARQL endpoint.</param>
        /// <param name="query">The SPARQL query to execute.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the query result as a string.</returns>
        /// <exception cref="HttpRequestException">The HTTP request failed.</exception> 
        public static async Task<HttpResponseMessage> Query(string endpointUrl = "", string query = "")
        {
            var response = await HttpClient.GetAsync($"{endpointUrl}?query={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();
            return response;
        }

        /// <summary>
        /// Executes a SPARQL update against the specified endpoint URL.
        /// </summary>
        /// <param name="endpointUrl">The URL of the SPARQL endpoint.</param>
        /// <param name="update">The SPARQL update to execute.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the update was successful.</returns>
        /// <exception cref="HttpRequestException">The HTTP request failed.</exception>
        public static async Task<HttpResponseMessage> Update(string endpointUrl = "", string update = "")
        {
            FormUrlEncodedContent content = new(new[]
            {
                    new KeyValuePair<string, string>("update", update)
                });
            var response = await HttpClient.PostAsync(endpointUrl, content);
            response.EnsureSuccessStatusCode();
            return response;
        }
    }
}