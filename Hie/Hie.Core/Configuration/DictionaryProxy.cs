using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Hie.Core.Configuration
{
	/// <summary>
	///     Proxy class to permit XML Serialization of generic dictionaries
	/// </summary>
	public class DictionaryProxy
	{
		public DictionaryProxy(IDictionary<string, string> original)
		{
			Original = original;
		}

		public DictionaryProxy()
		{
		}

		[XmlIgnore]
		public string this[string key]
		{
			get { return Original[key]; }
			set { Original[key] = value; }
		}

		[XmlIgnore]
		public IDictionary<string, string> Original { get; set; }

		public void Add(string key, string value)
		{
			Original.Add(key, value);
		}

		public class KeyAndValue
		{
			[XmlAttribute("name")]
			public string Key { get; set; }

			[XmlIgnore]
			public string Value { get; set; }

			[XmlElement("Value")]
			public XmlCDataSection CdataValue
			{
				get { return new XmlDocument().CreateCDataSection(Value.ToString()); }
				set { Value = value.Value as string; }
			}
		}

		// This field will store the deserialized list
		[XmlIgnore] private List<KeyAndValue> _list = new List<KeyAndValue>();

		[XmlElement("Property")]
		public List<KeyAndValue> KeysAndValues
		{
			get
			{
				// On deserialization, Original will be null, just return what we have
				if (Original == null)
				{
					return _list;
				}

				// If Original was present, add each of its elements to the list
				_list.Clear();
				foreach (var pair in Original)
				{
					_list.Add(new KeyAndValue { Key = pair.Key, Value = pair.Value });
				}

				return _list;
			}
		}

		/// <summary>
		///     Convenience method to return a dictionary from this proxy instance
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, string> ToDictionary()
		{
			return KeysAndValues.ToDictionary(key => key.Key, value => value.Value);
		}
	}
}
