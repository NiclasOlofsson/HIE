using System.IO;
using System.Xml;

namespace Hie.Core.Model
{
	public class XmlDisassembler : IDisassembler
	{
		private MemoryStream _stream = new MemoryStream();

		public void Initialize(IOptions options)
		{
			throw new System.NotImplementedException();
		}

		public void Disassemble(byte[] data)
		{
			// Reset the stream, don't creat a new
			_stream.Position = 0;
			_stream.Write(data, 0, data.Length);
			_stream.SetLength(data.Length);
			_stream.Position = 0;
		}

		public Message NextMessage()
		{
			if (_stream.Position == _stream.Length) return null;

			// This implementation doesn't really do that much since
			// it doesn't do envelope splitting yet. 
			var message = new Message("text/xml");

			// Change this to use XmlReader in order to set the original encoding of the stream
			// Actually, this string representation should really go away in the end. Message.Value 
			// doesn't have a place in the future.
			XmlDocument document = new XmlDocument();
			document.Load(_stream);
			message.SetValueFrom(document.OuterXml);

			return message;
		}
	}
}
