namespace Hie.Core.Model
{
	public interface IEndpoint
	{
		Channel DirectTarget { get; set; }
		IApplicationHost HostService { get; set; }

		void Initialize(IOptions options);

		void StartProcessing();

		void StopProcessing();

		void ProcessMessage(object source, Message message);

		void ProcessMessage(IEndpoint endpoint, byte[] data);
	}
}
