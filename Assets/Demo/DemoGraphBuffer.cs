using Sven.GraphManagement;

namespace Sven.Demo
{
    public class DemoGraphBuffer : GraphBuffer
    {
        public new void Awake()
        {
            graphName = DemoManager.graphName;
            base.Awake();
        }
    }
}
