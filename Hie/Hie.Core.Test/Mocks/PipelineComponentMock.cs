using Hie.Core.Model;

namespace Hie.Core.Mocks
{
	public class PipelineComponentMock : IDisassembler, IDecoder
	{
		public byte[] Decode(byte[] data)
		{
			return null;
		}

		public void Initialize(IOptions options)
		{
		}

		public void Disassemble(byte[] data)
		{
		}

		public Message NextMessage()
		{
			return null;
		}
	}
}