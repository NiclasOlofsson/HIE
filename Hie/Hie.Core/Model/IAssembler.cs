namespace Hie.Core.Model
{
	public interface IAssembler : IPipelineComponent
	{
		void AddMessage(Message message);
		byte[] Assemble();
	}
}
