using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

public static class GraphBuffer
{
    private static readonly IGraph graph = new Graph();

    public static IUriNode CreateUriNode(string uri)
    {
        return graph.CreateUriNode(UriFactory.Create(uri));
    }

    public static ILiteralNode CreateLiteralNode(string value, string language = null)
    {
        return graph.CreateLiteralNode(value, language);
    }


    public static ILiteralNode CreateLiteralNode(string value, System.Uri datatype)
    {
        return graph.CreateLiteralNode(value, datatype);
    }

    private static readonly HttpClient client = new HttpClient();

    // Start is called before the first frame update
    /*async void Start()
    {
        IGraph g = new Graph();

        IUriNode dotNetRDF = g.CreateUriNode(UriFactory.Create("http://www.dotnetrdf.org"));
        IUriNode says = g.CreateUriNode(UriFactory.Create("http://example.org/says"));
        ILiteralNode helloWorld = g.CreateLiteralNode("Hello World");
        ILiteralNode bonjourMonde = g.CreateLiteralNode("Bonjour tout le Monde", "fr");

        g.Assert(new Triple(dotNetRDF, says, helloWorld));
        g.Assert(new Triple(dotNetRDF, says, bonjourMonde));

        foreach (Triple t in g.Triples)
        {
            Debug.Log(t.ToString());
        }

        await SendRdfToEndpoint(g);
    }

    private async Task SendRdfToEndpoint(IGraph graph)
    {
        var writer = new CompressingTurtleWriter();
        StringBuilder sb = new StringBuilder();
        writer.Save(graph, new System.IO.StringWriter(sb));

        var content = new StringContent(sb.ToString(), Encoding.UTF8, "text/turtle");
        var response = await client.PostAsync("http://localhost:9999/blazegraph/namespace/test/sparql", content);

        if (response.IsSuccessStatusCode)
        {
            Debug.Log("Data successfully sent to the endpoint.");
        }
        else
        {
            Debug.LogError("Failed to send data to the endpoint.");
        }
    }*/
}