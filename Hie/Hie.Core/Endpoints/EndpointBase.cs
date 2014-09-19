using Hie.Core.Model;

namespace Hie.Core.Endpoints
{
	public abstract class EndpointBase : IEndpoint
	{
		public abstract void Initialize(IApplicationHost host, IOptions options);

		public abstract void StartProcessing();
		public abstract void StopProcessing();

		public abstract void ProcessMessage(IEndpoint endpoint, byte[] data);
	}
}
