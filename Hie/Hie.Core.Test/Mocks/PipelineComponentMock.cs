using Hie.Core.Endpoints;
using Hie.Core.Model;

namespace Hie.Core.Mocks
{
	public class PipelineComponentMock : IDisassembler, IDecoder, IEncoder
	{
		public byte[] Decode(byte[] data)
		{
			return null;
		}

		public void Initialize(IOptions options)
		{
		}

		public byte[] Encode(byte[] data)
		{
			return null;
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
