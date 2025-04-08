using Sven.GraphManagement;
using UnityEngine;

namespace Sven.Demo
{
    public class DemoGraphBuffer : GraphBuffer
    {
        public new void Awake()
        {
            graphName = DemoManager.graphName;
            endpoint = DemoManager.EndpointUri.ToString() + "/rdf-graphs/service";
            base.Awake();
        }
    }
}
