using System;
using System.Net.Http;
using System.Threading.Tasks;
using Sven.GraphManagement;
using UnityEngine;
using VDS.RDF.Query;

namespace Sven.Demo
{
    public class DemoGraphReader : GraphReader
    {
        public new void Awake()
        {
            readingMode = GraphStorageMode.Remote;
            graphName = DemoManager.graphName;
            endpoint = DemoManager.EndpointUri.ToString();
            base.Awake();
        }
    }
}
