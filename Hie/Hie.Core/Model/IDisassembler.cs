namespace Hie.Core.Model
{
	public interface IDisassembler : IPipelineComponent
	{
		void Disassemble(byte[] data);
		Message NextMessage();
	}
}
