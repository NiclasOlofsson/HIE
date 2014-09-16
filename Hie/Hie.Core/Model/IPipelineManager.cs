namespace Hie.Core.Model
{
	public interface IPipelineManager
	{
		void AddPipelineComponent(IEndpoint endpoint, IPipelineComponent pipelineComponent);
		void PushPipelineData(IEndpoint endpoint, byte[] data);
	}
}