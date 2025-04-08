using Sven.GraphManagement;

namespace Sven.Demo
{
    public class DemoGraphBuffer : GraphBuffer
    {
        public new void Awake()
        {
            graphName = DemoManager.graphName;
            endpoint = DemoManager.EndpointUri.ToString() + "/rdf-graphs/service";
            instantPerSecond = DemoManager.semantisationFrequency;
            base.Awake();
        }
    }
}
