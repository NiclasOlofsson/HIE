using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Hie.Core.Model
{
	public class XmlToHl7Converter
	{
		private const char ID_DELIMETER = '.';
		private const String MESSAGE_ROOT_ID = "HL7Message";

		private String _segmentSeparator;
		private String _fieldSeparator;
		private String _repetitionSeparator;
		private String _escapeCharacter;
		private String _componentSeparator;
		private String _subcomponentSeparator;
		private bool _encodeEntities = false;
		private bool _inElement = false;
		private int _rootLevel = -1;

		private int _previousDelimeterCount = -1;
		private int _previousDelimiterLength = 1;
		private String[] _previousFieldNameArray;
		private String[] _previousComponentNameArray;
		private String[] _previousSubcomponentNameArray;

		private StringBuilder _output = new StringBuilder();

		public XmlToHl7Converter(String segmentSeparator, String fieldSeparator, String componentSeparator, String repetitionSeparator, String escapeCharacter, String subcomponentSeparator, bool encodeEntities)
		{
			_segmentSeparator = segmentSeparator;
			_fieldSeparator = fieldSeparator;
			_componentSeparator = componentSeparator;
			_repetitionSeparator = repetitionSeparator;
			_escapeCharacter = escapeCharacter;
			_subcomponentSeparator = subcomponentSeparator;
			_encodeEntities = encodeEntities;
		}


		public byte[] Convert(XDocument document)
		{
			XmlReader reader = document.CreateReader();

			while (reader.Read())
			{
				switch (reader.NodeType)
				{
					case XmlNodeType.Element:
						StartElement(reader.LocalName);
						if (reader.IsEmptyElement)
						{
							EndElement(reader.LocalName);
						}
						break;
					case XmlNodeType.EndElement:
						EndElement(reader.LocalName);
						break;
					case XmlNodeType.Text:
						Characters(reader.Value);
						break;
					default:
						break;
				}
			}

			return Encoding.UTF8.GetBytes(_output.ToString());
		}

		public void StartElement(String localName)
		{
			_inElement = true;

			String[] localNameArray = localName.Split(new[] { ID_DELIMETER });

			if (_rootLevel == -1)
			{
				_rootLevel = localNameArray.Length;
			}

			/*
         * Skip the root element, MSH.1, and MSH.2 since those don't have any data that we care
         * about.
         */
			if ((localNameArray.Length == 1) && (localNameArray[0].Equals(MESSAGE_ROOT_ID)))
			{
				_rootLevel = 0;
				return;
			}
			else if (localNameArray.Length == 2)
			{
				if (IsHeaderSegment(localNameArray[0]))
				{
					if ((localNameArray[1].Length == 1) && (localNameArray[1][0] == '1' || localNameArray[1][0] == '2'))
					{
						_previousFieldNameArray = localNameArray;
						return;
					}
				}
			}

			/*
         * If the element that we've found is the same as the last, then we have a repetition, so we
         * remove the last separator that was added and append to repetition separator.
         */
			if (_previousFieldNameArray != null && localNameArray.SequenceEqual(_previousFieldNameArray))
			{
				_output.Remove(_output.Length - 1, 1);
				_output.Append(_repetitionSeparator);
				_previousComponentNameArray = null;
				return;
			}

			/*
         * To find the delimeter count we are splitting the element name by the ID delimeter.
         */
			int currentDelimeterCount = localNameArray.Length - 1;

			/*
         * MIRTH-2078: Don't add missing fields/components/subcomponents if the current level was
         * the starting level. This only pertains to partial XML messages where the root is a field
         * or component.
         */
			if (currentDelimeterCount == 1 && _rootLevel <= 1)
			{
				/*
             * This will add missing fields if any (ex. between OBX.1 and OBX.5).
             */
				int previousFieldId = 0;

				if (_previousFieldNameArray != null)
				{
					previousFieldId = Int32.Parse(_previousFieldNameArray[1]);
				}

				int currentFieldId = Int32.Parse(localNameArray[1]);

				for (int i = 1; i < (currentFieldId - previousFieldId); i++)
				{
					_output.Append(_fieldSeparator);
				}

				_previousFieldNameArray = localNameArray;
			}
			else if (currentDelimeterCount == 2 && _rootLevel <= 2)
			{
				/*
             * This will add missing components if any (ex. between OBX.1.1 and OBX.1.5).
             */
				int previousComponentId = 0;

				if (_previousComponentNameArray != null)
				{
					previousComponentId = Int32.Parse(_previousComponentNameArray[2]);
				}

				int currentComponentId = Int32.Parse(localNameArray[2]);

				for (int i = 1; i < (currentComponentId - previousComponentId); i++)
				{
					_output.Append(_componentSeparator);
					_previousDelimiterLength = _componentSeparator.Length;
				}

				_previousComponentNameArray = localNameArray;
			}
			else if (currentDelimeterCount == 3 && _rootLevel <= 3)
			{
				/*
             * This will add missing subcomponents if any (ex. between OBX.1.1.1 and OBX.1.1.5).
             */
				int previousSubcomponentId = 0;

				if (_previousSubcomponentNameArray != null)
				{
					previousSubcomponentId = Int32.Parse(_previousSubcomponentNameArray[3]);
				}

				int currentSubcomponentId = Int32.Parse(localNameArray[3]);

				for (int i = 1; i < (currentSubcomponentId - previousSubcomponentId); i++)
				{
					_output.Append(_subcomponentSeparator);
					_previousDelimiterLength = _subcomponentSeparator.Length;
				}

				_previousSubcomponentNameArray = localNameArray;
			}

			/*
         * If we have an element with no periods, then we know its the name of the segment, so write
         * it to the output buffer followed by the field separator.
         */
			if (currentDelimeterCount == 0)
			{
				_output.Append(localName);
				_output.Append(_fieldSeparator);

				/*
             * Also set previousFieldName to null so that multiple segments in a row with only one
             * field don't trigger a repetition character. (i.e. NTE|1<CR>NTE|2)
             */
				_previousFieldNameArray = null;
			}
			else if (currentDelimeterCount == 1)
			{
				_previousComponentNameArray = null;
			}
			else if (currentDelimeterCount == 2)
			{
				_previousSubcomponentNameArray = null;
			}
		}

		public void EndElement(String localName)
		{
			_inElement = false;

			String[] localNameArray = localName.Split(ID_DELIMETER);

			/*
         * Once we see the closing of MSH.1 or MSH.2 tags, we know that the separator characters
         * have been added to the output buffer, so we can grab them and set the local variables.
         */
			if ((localNameArray.Length == 1) && (localNameArray[0].Equals(MESSAGE_ROOT_ID)))
			{
				return;
			}
			else if (localNameArray.Length == 2)
			{
				if (IsHeaderSegment(localNameArray[0]))
				{
					if ((localNameArray[1].Length == 1) && (localNameArray[1][0] == '1'))
					{
						_output = _output.Replace("&amp;", "&");
						_fieldSeparator = _output[_output.Length - 1].ToString();
						return;
					}
					else if ((localNameArray[1].Length == 1) && (localNameArray[1][0] == '2'))
					{
						_output = _output.Replace("&amp;", "&");
						string separators = _output.ToString(4, _output.Length - 4);
						_componentSeparator = separators[0].ToString();
						_repetitionSeparator = separators[1].ToString();
						_escapeCharacter = separators.Length > 2 ? separators[2].ToString() : "";
						_subcomponentSeparator = separators.Length > 3 ? separators[3].ToString() : "";
					}
				}
			}

			int currentDelimeterCount = localNameArray.Length - 1;

			/*
         * We don't want to have trailing separators, so once we get to the last element of a nested
         * level, we delete the last character.
         */
			if (currentDelimeterCount > _previousDelimeterCount)
			{
				_previousDelimeterCount = currentDelimeterCount;
			}
			else if (currentDelimeterCount < _previousDelimeterCount && _previousDelimiterLength > 0)
			{
				_output.Remove(_output.Length - 1, 1);
				_previousDelimeterCount = currentDelimeterCount;
			}

			/*
         * The number of periods in the element tells us the level. So, MSH is at level 0, MSH.3 is
         * at level 1, MSH.3.1 at level 2, and so on. We can use this to determine which seperator
         * to append once the element is closed.
         * 
         * MIRTH-2078: Only add the last character if the root delimiter is 0 (HL7Message) or the
         * current element level is deeper than the root level. This only pertains to partial XML
         * messages where the root is a field or component.
         */
			if (_rootLevel == 0 || currentDelimeterCount >= _rootLevel)
			{
				switch (currentDelimeterCount)
				{
					case 0:
						_output.Append(_segmentSeparator);
						break;
					case 1:
						_output.Append(_fieldSeparator);
						break;
					case 2:
						_output.Append(_componentSeparator);
						_previousDelimiterLength = _componentSeparator.Length;
						break;
					case 3:
						_output.Append(_subcomponentSeparator);
						_previousDelimiterLength = _subcomponentSeparator.Length;
						break;
					default:
						break;
				}
			}
		}

		public void Characters(string text)
		{
			/*
         * Write the substring to the output buffer, unless it is the field separators (to avoid
         * MSH.1. being written out).
         */
			if (_inElement && !text.Equals(_fieldSeparator))
			{
				_output.Append(text);
			}
		}

		private bool IsHeaderSegment(String segmentName)
		{
			if (segmentName.Length != 3) return false;

			return segmentName.StartsWith("MSH") || segmentName.StartsWith("MSB") || segmentName.StartsWith("MSF");

			//return ((segmentName[0] == 'M') && (segmentName[1] == 'S') && (segmentName[2] == 'H')) 
			//	|| ((segmentName[1] == 'H') && (segmentName[2] == 'S') && ((segmentName[0] == 'B') || (segmentName[0] == 'F')));
		}
	}
}