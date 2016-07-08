//
// YourApp.Network.MockRequest.cs: This class basically acts as a dummy request class. As you can see in the overriden 
// Start() method we're actually calling some of the lifecycle methods of the Request class.
//
// Author:
//   Agustin Larghi (agustin.tomas.larghi@hotmail.com)
//

using System.Threading.Tasks;

namespace YourApp
{
    public class MockRequest<T> : Request<T>
    {
        private T result;

        public MockRequest<T> SetResult(T result)
        {
            this.result = result;
            return this;
        }

        public override Task<T> Start()
        {
            onRequestStarted?.Invoke();
            onSuccess?.Invoke(result);
            onRequestCompleted?.Invoke();
            return Task.FromResult(result);
        }
    }
}
