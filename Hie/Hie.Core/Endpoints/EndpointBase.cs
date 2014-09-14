using Hie.Core.Model;

namespace Hie.Core.Endpoints
{
	public enum EndpointDirection
	{
		Unknown,
		OneWayReceive,
		OneWaySend,
		RequestResponseSend,
		RequestResponseReceive
	}

	public abstract class EndpointBase : IEndpoint
	{
		public Channel DirectTarget { get; set; }
		public ApplicationHost HostService { get; set; }

		public void Init(IOptions options)
		{
		}

		public abstract void StartProcessing();
		public abstract void StopProcessing();

		public abstract void ProcessMessage(object source, Message message);
	}
}
