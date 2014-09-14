using Hie.Core.Endpoints;

namespace Hie.Core.Model
{
	public interface IEndpoint
	{
		Channel DirectTarget { get; set; }
		ApplicationHost HostService { get; set; }

		void Init(IOptions options);

		void StartProcessing();

		void StopProcessing();

		void ProcessMessage(object source, Message message);
	}
}
