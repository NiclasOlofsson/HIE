using System;

namespace Hie.Core.Model
{
	public class Hl7Assembler : IAssembler
	{
		private Message _message;

		public void Initialize(IOptions options)
		{
		}

		public void AddMessage(Message message)
		{
			_message = message;
		}

		public byte[] Assemble()
		{
			String segmentSeparator = "\r";
			String fieldSeparator = "|";
			String componentSeparator = "^";
			String repetitionSeparator = "~";
			String subcomponentSeparator = "&";
			String escapeCharacter = "\\";

			var converter = new XmlToHl7Converter(segmentSeparator, fieldSeparator, componentSeparator, repetitionSeparator, escapeCharacter, subcomponentSeparator, true);
			byte[] result = converter.Convert(_message.GetXDocument());

			return result;
		}
	}
}