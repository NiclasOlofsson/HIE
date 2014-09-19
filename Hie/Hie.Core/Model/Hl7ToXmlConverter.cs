using System.Text.RegularExpressions;
using System.Xml;

namespace Hie.Core.Model
{
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
				if (i == 0) sHl7Line = sHl7Line.Insert(3, "|");
				string[] sFields = GetMessgeFields(sHl7Line);

				// Create a new element in the XML for the line
				string segmentName = sFields[0];
				XmlElement segmentElement = _xmlDoc.CreateElement(segmentName);
				_xmlDoc.DocumentElement.AppendChild(segmentElement);

				// For each field in the line of HL7
				for (int a = 1; a < sFields.Length; a++)
				{
					// Part of the HL7 specification is that part
					// of the message header defines which characters
					// are going to be used to delimit the message
					// and since we want to capture the field that
					// contains those characters we need
					// to just capture them and stick them in an element.
					if (sFields[a] == @"^~\&")
					{
						XmlElement fieldElement = _xmlDoc.CreateElement(string.Format("{0}.{1}", segmentName, 1));
						fieldElement.InnerText = "|";
						segmentElement.RemoveAll(); // This will be the first
						segmentElement.AppendChild(fieldElement);

						fieldElement = _xmlDoc.CreateElement(string.Format("{0}.{1}", segmentName, 2));
						fieldElement.InnerText = sFields[a];
						segmentElement.AppendChild(fieldElement);
						continue;
					}

					// Check for repetition
					string[] rFields = GetRepetitions(sFields[a]);
					for (int r = 0; r < rFields.Length; r++)
					{
						// Create a new element
						XmlElement fieldElement = _xmlDoc.CreateElement(string.Format("{0}.{1}", segmentName, a));

						// Get the components within this field, separated by carats (^)
						// If there are more than one, go through and create an element for
						// each, then check for subcomponents, and repetition in both.
						string[] sComponents = GetComponents(rFields[r]);
						if (sComponents.Length == 1)
						{
							XmlElement componentEl = _xmlDoc.CreateElement(string.Format("{0}.{1}.{2}", segmentName, a, 1));
							if (!string.IsNullOrEmpty(rFields[r]))
							{
								string innerText = rFields[r];
								SetText(componentEl, innerText);
								fieldElement.AppendChild(componentEl);
							}
							segmentElement.AppendChild(fieldElement);
							continue;
						}

						for (int b = 0; b < sComponents.Length; b++)
						{
							XmlElement componentEl = _xmlDoc.CreateElement(string.Format("{0}.{1}.{2}", segmentName, a, b + 1));

							string[] subComponents = GetSubComponents(sComponents[b]);
							if (subComponents.Length > 1)
								// There were subcomponents
							{
								for (int c = 0; c < subComponents.Length; c++)
								{
									XmlElement subComponentEl = _xmlDoc.CreateElement(string.Format("{0}.{1}.{2}.{3}", segmentName, a, b + 1, c + 1));
									subComponentEl.InnerText = subComponents[c];
									componentEl.AppendChild(subComponentEl);
								}
								fieldElement.AppendChild(componentEl);
							}
							else // There were no subcomponents
							{
								string[] sRepetitions = GetRepetitions(sComponents[b]);
								if (sRepetitions.Length > 1)
								{
									for (int c = 0; c < sRepetitions.Length; c++)
									{
										XmlElement repetitionEl = _xmlDoc.CreateElement(string.Format("{0}.{1}.{2}.{3}", segmentName, a, b + 1, c + 1));
										repetitionEl.InnerText = sRepetitions[c];
										componentEl.AppendChild(repetitionEl);
									}
									fieldElement.AppendChild(componentEl);
									segmentElement.AppendChild(fieldElement);
								}
								else
								{
									if (!string.IsNullOrEmpty(sComponents[b]))
									{
										componentEl.InnerText = sComponents[b];
									}
									fieldElement.AppendChild(componentEl);
									segmentElement.AppendChild(fieldElement);
								}
							}
						}
						segmentElement.AppendChild(fieldElement);
					}
				}
			}

			return _xmlDoc;
		}

		private static void SetText(XmlElement componentEl, string innerText)
		{
			if (string.IsNullOrWhiteSpace(innerText))
			{
				componentEl.SetAttribute("xml:space", "preserve");
			}
			componentEl.InnerText = innerText;
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
			output.PreserveWhitespace = true;
			XmlElement rootNode = output.CreateElement("HL7Message");
			output.AppendChild(rootNode);
			return output;
		}
	}
}
