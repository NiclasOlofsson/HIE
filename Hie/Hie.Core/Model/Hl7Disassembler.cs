using System.IO;
using System.Text;
using System.Xml;

namespace Hie.Core.Model
{
	public class Hl7Disassembler : IDisassembler
	{
		private MemoryStream _stream = new MemoryStream();

		public void Initialize(IOptions options)
		{
			throw new System.NotImplementedException();
		}

		public void Disassemble(byte[] data)
		{
			_stream.Position = 0;
			_stream.Write(data, 0, data.Length);
			_stream.SetLength(data.Length);
			_stream.Position = 0;
		}

		public Message NextMessage()
		{
			if (_stream.Position == _stream.Length) return null;

			// Decode and convert ER7 data to XML
			string hl7 = Encoding.UTF8.GetString(_stream.ToArray());
			XmlDocument document = Hl7ToXmlConverter.ConvertToXml(hl7);

			Message message = new Message("text/xml");
			//message.SetValueFrom(document.OuterXml);

			MemoryStream ms = new MemoryStream();
			document.Save(ms);
			ms.Position = 0;
			message.Stream = ms;

			_stream.Seek(0, SeekOrigin.End); // We consumed all, so reflect it on stream

			return message;
		}
	}
}
