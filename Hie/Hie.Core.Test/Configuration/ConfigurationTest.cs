using System.Collections.Generic;
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
		public void ApplicationConfigurationSerializationRountripTest()
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
					DictionaryProxy<string> options = new DictionaryProxy<string>(new Dictionary<string, string>());
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

			config.Save("CreateConfigurationTest-output.xml");

			//NOTE: EXPORT XML SCHEMA
			config.SaveSchema("CreateConfigurationTest-output.xsd");

			// Read and assert

			ApplicationConfiguration resultConfig = ApplicationConfiguration.Load("CreateConfigurationTest-output.xml");

			Assert.IsNotNull(resultConfig);
			Assert.IsNotNull(resultConfig.Endpoints);
			Assert.AreEqual(2, resultConfig.Endpoints.Count());

			Assert.IsNotNull(resultConfig.Channels);
			Assert.AreEqual(1, resultConfig.Channels.Count());

			Assert.IsNotNull(resultConfig.Channels.First().Destinations);
			Assert.AreEqual(1, resultConfig.Channels.First().Destinations.Count());

			Assert.IsNotNull(resultConfig.Channels.First().Destinations.First().Transformers);
			Assert.AreEqual(1, resultConfig.Channels.First().Destinations.First().Transformers.Count());

			Assert.IsNotNull(resultConfig.Channels.First().Destinations.First().Filters);
			Assert.AreEqual(1, resultConfig.Channels.First().Destinations.First().Filters.Count());

			Assert.IsNotNull(resultConfig.Channels.First().Source);

			Assert.IsNotNull(resultConfig.Channels.First().Source.Filters);
			Assert.AreEqual(1, resultConfig.Channels.First().Source.Filters.Count());

			Assert.IsNotNull(resultConfig.Channels.First().Source.Transformers);
			Assert.AreEqual(1, resultConfig.Channels.First().Source.Transformers.Count());

			// Options
			Assert.IsNotNull(resultConfig.Channels.First().Source.Filters.First().Options);
			Dictionary<string, string> deserializedProperties = resultConfig.Channels.First().Source.Filters.First().Options.ToDictionary();
			Assert.IsNotNull(deserializedProperties);
			Assert.AreEqual(3, deserializedProperties.Count());
			Assert.AreEqual("value1&", deserializedProperties["property1"]);
		}

		[TestMethod]
		public void CreateApplicationFromApplicationConfigurationTest()
		{
			ApplicationConfiguration config = ApplicationConfiguration.Load("CreateApplicationFromApplicationConfigurationTest.xml");

			Application application = config.CreateApplication();

			Assert.IsNotNull(application);
			Assert.AreEqual("MyApplication", application.Name);
			Assert.AreEqual("A test application for TDD HIE", application.Description);

			Assert.IsNotNull(application);
			Assert.IsNotNull(application.Endpoints);
			Assert.AreEqual(2, application.Endpoints.Count());
			Assert.AreEqual(typeof (EndpointMock), application.Endpoints.First().GetType());
			Assert.AreEqual(typeof (EndpointMock), application.Endpoints.Last().GetType());

			Assert.IsNotNull(application.Channels);
			Assert.AreEqual(1, application.Channels.Count());

			Assert.IsNotNull(application.Channels.First().Destinations);
			Assert.AreEqual(1, application.Channels.First().Destinations.Count());

			Assert.IsNotNull(application.Channels.First().Destinations.First().Filters);
			Assert.AreEqual(1, application.Channels.First().Destinations.First().Filters.Count());
			Assert.AreEqual(typeof (DelegateFilter), application.Channels.First().Destinations.First().Filters.First().GetType());

			Assert.IsNotNull(application.Channels.First().Destinations.First().Transformers);
			Assert.AreEqual(1, application.Channels.First().Destinations.First().Transformers.Count());
			Assert.AreEqual(typeof (DelegateTransformer), application.Channels.First().Destinations.First().Transformers.First().GetType());

			Assert.IsNotNull(application.Channels.First().Source);

			Assert.IsNotNull(application.Channels.First().Source.Filters);
			Assert.AreEqual(1, application.Channels.First().Source.Filters.Count());
			Assert.AreEqual(typeof (DelegateFilter), application.Channels.First().Source.Filters.First().GetType());

			Assert.IsNotNull(application.Channels.First().Source.Transformers);
			Assert.AreEqual(1, application.Channels.First().Source.Transformers.Count());
			Assert.AreEqual(typeof (DelegateTransformer), application.Channels.First().Source.Transformers.First().GetType());
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
		public DictionaryProxy<string> Options { get; set; }
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
	/// <typeparam name="string">The type of the key</typeparam>
	/// <typeparam name="V">The type of the value</typeparam>
	public class DictionaryProxy<V> where V : class
	{
		public DictionaryProxy(IDictionary<string, V> original)
		{
			Original = original;
		}

		public DictionaryProxy()
		{
		}

		[XmlIgnore]
		public V this[string key]
		{
			get { return Original[key]; }
			set { Original[key] = value; }
		}

		[XmlIgnore]
		public IDictionary<string, V> Original { get; set; }

		public void Add(string key, V value)
		{
			Original.Add(key, value);
		}

		public class KeyAndValue
		{
			[XmlAttribute("name")]
			public string Key { get; set; }

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
		public Dictionary<string, V> ToDictionary()
		{
			return KeysAndValues.ToDictionary(key => key.Key, value => value.Value);
		}
	}
}
