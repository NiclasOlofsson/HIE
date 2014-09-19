namespace Hie.Core.Model
{
	public interface IEndpoint
	{
		void Initialize(IApplicationHost host, IOptions options);

		void StartProcessing();

		void StopProcessing();

		void ProcessMessage(IEndpoint endpoint, byte[] data);
	}
}
