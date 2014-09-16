using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
			message.Value = document.OuterXml;

			MemoryStream ms = new MemoryStream();
			document.Save(ms);
			ms.Position = 0;
			message.Stream = ms;

			_stream.Seek(0, SeekOrigin.End); // We consumed all, so reflect it on stream

			return message;
		}
	}

	// SOURCE: http://www.codeproject.com/Articles/29670/Converting-HL-to-XML


	public static class Hl7ToXmlConverter
	{
		private static XmlDocument _xmlDoc;

		public static XmlDocument ConvertToXml(string sHL7)
		{
			// Go and create the base XML
			_xmlDoc = CreateXmlDoc();

			// HL7 message segments are terminated by carriage returns,
			// so to get an array of the message segments, split on carriage return
			string[] sHl7Lines = sHL7.Split('\r');

			// Now we want to replace any other unprintable control
			// characters with whitespace otherwise they'll break the XML
			for (int i = 0; i < sHl7Lines.Length; i++)
			{
				sHl7Lines[i] = Regex.Replace(sHl7Lines[i], @"[^ -~]", "");
			}

			// Go through each segment in the message
			// and first get the fields, separated by pipe (|),
			// then for each of those, get the field components,
			// separated by carat (^), and check for
			// repetition (~) and also check each component
			// for subcomponents, and repetition within them too.
			for (int i = 0; i < sHl7Lines.Length; i++)
			{
				// Don't care about empty lines
				if (sHl7Lines[i] == string.Empty) continue;

				// Get the line and get the line's segments
				string sHl7Line = sHl7Lines[i];
				string[] sFields = GetMessgeFields(sHl7Line);

				// Create a new element in the XML for the line
				XmlElement el = _xmlDoc.CreateElement(sFields[0]);
				_xmlDoc.DocumentElement.AppendChild(el);

				// For each field in the line of HL7
				for (int a = 0; a < sFields.Length; a++)
				{
					// Create a new element
					XmlElement fieldEl = _xmlDoc.CreateElement(string.Format("{0}.{1}", sFields[0], a));

					// Part of the HL7 specification is that part
					// of the message header defines which characters
					// are going to be used to delimit the message
					// and since we want to capture the field that
					// contains those characters we need
					// to just capture them and stick them in an element.
					if (sFields[a] != @"^~\&")
					{
						// Get the components within this field, separated by carats (^)
						// If there are more than one, go through and create an element for
						// each, then check for subcomponents, and repetition in both.
						string[] sComponents = GetComponents(sFields[a]);
						if (sComponents.Length > 1)
						{
							for (int b = 0; b < sComponents.Length; b++)
							{
								XmlElement componentEl = _xmlDoc.CreateElement(string.Format("{0}.{1}.{2}", sFields[0], a, b));

								string[] subComponents = GetSubComponents(sComponents[b]);
								if (subComponents.Length > 1)
									// There were subcomponents
								{
									for (int c = 0; c < subComponents.Length; c++)
									{
										// Check for repetition
										string[] subComponentRepetitions =
											GetRepetitions(subComponents[c]);
										if (subComponentRepetitions.Length > 1)
										{
											for (int d = 0;
												d < subComponentRepetitions.Length;
												d++)
											{
												XmlElement subComponentRepEl = _xmlDoc.CreateElement(string.Format("{0}.{1}.{2}.{3}.{4}", sFields[0], a, b, c, d));
												subComponentRepEl.InnerText = subComponentRepetitions[d];
												componentEl.AppendChild(subComponentRepEl);
											}
										}
										else
										{
											XmlElement subComponentEl = _xmlDoc.CreateElement(string.Format("{0}.{1}.{2}.{3}", sFields[0], a, b, c));
											subComponentEl.InnerText = subComponents[c];
											componentEl.AppendChild(subComponentEl);
										}
									}
									fieldEl.AppendChild(componentEl);
								}
								else // There were no subcomponents
								{
									string[] sRepetitions =
										Hl7ToXmlConverter.GetRepetitions(sComponents[b]);
									if (sRepetitions.Length > 1)
									{
										for (int c = 0; c < sRepetitions.Length; c++)
										{
											XmlElement repetitionEl = _xmlDoc.CreateElement(string.Format("{0}.{1}.{2}.{3}", sFields[0], a, b, c));
											repetitionEl.InnerText = sRepetitions[c];
											componentEl.AppendChild(repetitionEl);
										}
										fieldEl.AppendChild(componentEl);
										el.AppendChild(fieldEl);
									}
									else
									{
										componentEl.InnerText = sComponents[b];
										fieldEl.AppendChild(componentEl);
										el.AppendChild(fieldEl);
									}
								}
							}
							el.AppendChild(fieldEl);
						}
						else
						{
							fieldEl.InnerText = sFields[a];
							el.AppendChild(fieldEl);
						}
					}
					else
					{
						fieldEl.InnerText = sFields[a];
						el.AppendChild(fieldEl);
					}
				}
			}

			return _xmlDoc;
		}

		private static string[] GetMessgeFields(string s)
		{
			return s.Split('|');
		}

		private static string[] GetComponents(string s)
		{
			return s.Split('^');
		}

		private static string[] GetSubComponents(string s)
		{
			return s.Split('&');
		}

		private static string[] GetRepetitions(string s)
		{
			return s.Split('~');
		}

		private static XmlDocument CreateXmlDoc()
		{
			XmlDocument output = new XmlDocument();
			XmlElement rootNode = output.CreateElement("HL7Message");
			output.AppendChild(rootNode);
			return output;
		}
	}
}
