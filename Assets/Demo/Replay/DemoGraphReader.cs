using Sven.GraphManagement;
using UnityEngine;

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
