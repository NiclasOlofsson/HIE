using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using Hie.Core.Mocks;
using Hie.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core.Configuration
{
	[TestClass]
	public class ConfigurationTest
	{
		[TestMethod]
		public void CreateConfigurationTest()
		{
			ApplicationConfiguration config = new ApplicationConfiguration();
			config.Name = "MyApplication";
			config.Description = "A test application for TDD HIE";

			{
				// Receive
				EndpointConfiguration endpoint = new EndpointConfiguration();
				var typeInfo = typeof (EndpointMock).GetTypeInfo();
				endpoint.TypeInfo = typeInfo.AssemblyQualifiedName;
				config.Endpoints.Add(endpoint);
			}

			{
				// Send
				EndpointConfiguration endpoint = new EndpointConfiguration();
				var typeInfo = typeof (EndpointMock).GetTypeInfo();
				endpoint.TypeInfo = typeInfo.AssemblyQualifiedName;
				config.Endpoints.Add(endpoint);
			}

			{
				ChannelConfiguration channel = new ChannelConfiguration();
				channel.Name = "Test channel";
				channel.Description = "A test channel";
				config.Channels.Add(channel);

				SourceConfiguration source = new SourceConfiguration();
				channel.Source = source;

				{
					FilterConfiguration filter = new FilterConfiguration();
					var typeInfo = typeof (DelegateFilter).GetTypeInfo();
					filter.TypeInfo = typeInfo.AssemblyQualifiedName;
					DictionaryProxy<string, string> options = new DictionaryProxy<string, string>(new Dictionary<string, string>());
					options.Add("property1", "value1&");
					options.Add("property2", "value2\n\nvalue2");
					options.Add("property3", "value3");
					filter.Options = options;
					source.Filters.Add(filter);
				}
				{
					TransformerConfiguration transformer = new TransformerConfiguration();
					var typeInfo = typeof (DelegateTransformer).GetTypeInfo();
					transformer.TypeInfo = typeInfo.AssemblyQualifiedName;
					source.Transformers.Add(transformer);
				}

				DestinationConfiguration destination = new DestinationConfiguration();
				channel.Destinations.Add(destination);
				{
					FilterConfiguration filter = new FilterConfiguration();
					var typeInfo = typeof (DelegateFilter).GetTypeInfo();
					filter.TypeInfo = typeInfo.AssemblyQualifiedName;
					destination.Filters.Add(filter);
				}
				{
					TransformerConfiguration transformer = new TransformerConfiguration();
					var typeInfo = typeof (DelegateTransformer).GetTypeInfo();
					transformer.TypeInfo = typeInfo.AssemblyQualifiedName;
					destination.Transformers.Add(transformer);
				}
			}

			XmlSerializer serializer = new XmlSerializer(typeof (ApplicationConfiguration));

			using (TextWriter writer = new StreamWriter("CreateConfigurationTest-output.xml"))
			{
				serializer.Serialize(writer, config);
			}

			ApplicationConfiguration resultConfig;

			using (TextReader sr = new StreamReader("CreateConfigurationTest-output.xml"))
			{
				resultConfig = (ApplicationConfiguration) serializer.Deserialize(sr);
			}

			Assert.IsNotNull(resultConfig);
			Assert.IsNotNull(resultConfig.Channels);
			Assert.IsNotNull(resultConfig.Channels.FirstOrDefault().Source);
			//Assert.IsNotNull(resultConfig.Channels.FirstOrDefault().Source.Filters.FirstOrDefault().Options.ToDictionary());

			Assert.AreEqual("value1&", resultConfig.Channels.FirstOrDefault().Source.Filters.FirstOrDefault().Options.ToDictionary().First().Value);
		}
	}

	public class DestinationConfiguration
	{
		public List<FilterConfiguration> Filters { get; private set; }
		public List<TransformerConfiguration> Transformers { get; private set; }

		public DestinationConfiguration()
		{
			Filters = new List<FilterConfiguration>();
			Transformers = new List<TransformerConfiguration>();
		}
	}

	public class FilterConfiguration
	{
		public string TypeInfo { get; set; }
		public DictionaryProxy<string, string> Options { get; set; }
	}

	public class TransformerConfiguration
	{
		public string TypeInfo { get; set; }
	}

	public class SourceConfiguration
	{
		public List<FilterConfiguration> Filters { get; private set; }
		public List<TransformerConfiguration> Transformers { get; private set; }

		public SourceConfiguration()
		{
			Filters = new List<FilterConfiguration>();
			Transformers = new List<TransformerConfiguration>();
		}
	}

	public class EndpointConfiguration
	{
		public string TypeInfo { get; set; }
	}

	public class ApplicationConfiguration
	{
		public string Name { get; set; }

		public string Description { get; set; }
		public List<EndpointConfiguration> Endpoints { get; private set; }
		public List<ChannelConfiguration> Channels { get; private set; }


		public ApplicationConfiguration()
		{
			Channels = new List<ChannelConfiguration>();
			Endpoints = new List<EndpointConfiguration>();
		}
	}

	public class ChannelConfiguration
	{
		public string Name { get; set; }

		public string Description { get; set; }
		public SourceConfiguration Source { get; set; }
		public List<DestinationConfiguration> Destinations { get; set; }

		public ChannelConfiguration()
		{
			Destinations = new List<DestinationConfiguration>();
		}
	}

	/// <summary>
	///     Proxy class to permit XML Serialization of generic dictionaries
	/// </summary>
	/// <typeparam name="K">The type of the key</typeparam>
	/// <typeparam name="V">The type of the value</typeparam>
	public class DictionaryProxy<K, V> where V : class
	{
		#region Construction and Initialization

		public DictionaryProxy(IDictionary<K, V> original)
		{
			Original = original;
		}

		/// <summary>
		///     Default constructor so deserialization works
		/// </summary>
		public DictionaryProxy()
		{
		}

		/// <summary>
		///     Use to set the dictionary if necessary, but don't serialize
		/// </summary>
		[XmlIgnore]
		public IDictionary<K, V> Original { get; set; }

		public void Add(K key, V value)
		{
			Original.Add(key, value);
		}

		[XmlIgnore]
		public V this[K key]
		{
			get { return Original[key]; }
			set { Original[key] = value; }
		}

		#endregion

		#region The Proxy List

		/// <summary>
		///     Holds the keys and values
		/// </summary>
		public class KeyAndValue
		{
			[XmlAttribute("name")]
			public K Key { get; set; }

			[XmlIgnore]
			public V Value { get; set; }

			[XmlElement("Value")]
			public XmlCDataSection CdataValue
			{
				get { return new XmlDocument().CreateCDataSection(Value.ToString()); }
				set { Value = value.Value as V; }
			}
		}

		// This field will store the deserialized list
		[XmlIgnore] private Collection<KeyAndValue> _list;

		/// <remarks>
		///     XmlElementAttribute is used to prevent extra nesting level. It's
		///     not necessary.
		/// </remarks>
		[XmlElement("Property")]
		public Collection<KeyAndValue> KeysAndValues
		{
			get
			{
				if (_list == null)
				{
					_list = new Collection<KeyAndValue>();
				}

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

		#endregion

		/// <summary>
		///     Convenience method to return a dictionary from this proxy instance
		/// </summary>
		/// <returns></returns>
		public Dictionary<K, V> ToDictionary()
		{
			return KeysAndValues.ToDictionary(key => key.Key, value => value.Value);
		}
	}
}
