using Hie.Core.Model;

namespace Hie.Core.Endpoints
{
	public abstract class EndpointBase : IEndpoint
	{
		public Channel DirectTarget { get; set; }
		public IApplicationHost HostService { get; set; }

		public abstract void Initialize(IOptions options);
		public abstract void StartProcessing();
		public abstract void StopProcessing();

		public abstract void ProcessMessage(object source, Message message);
		public abstract void ProcessMessage(IEndpoint endpoint, byte[] data);
	}
}
