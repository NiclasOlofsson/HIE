using Hie.Core.Model;

namespace Hie.Core.Mocks
{
	public class PipelineComponentMock : IDisassembler, IDecoder, IEncoder
	{
		private byte[] _data;

		public byte[] Decode(byte[] data)
		{
			return data;
		}

		public void Initialize(IOptions options)
		{
		}

		public byte[] Encode(byte[] data)
		{
			return data;
		}

		public void Disassemble(byte[] data)
		{
			_data = data;
		}

		public Message NextMessage()
		{
			if (_data != null)
			{
				Message message = new Message("");
				message.SetValueFrom(_data);
				_data = null;
				return message;
			}

			return null;
		}
	}
}
