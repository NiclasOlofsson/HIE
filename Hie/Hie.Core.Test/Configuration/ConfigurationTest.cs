using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hie.Core.Mocks;
using Hie.Core.Model;
using Hie.Core.Modules.JavaScript;
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
				PortConfiguration port = new PortConfiguration();
				config.Ports.Add(port);

				EndpointConfiguration endpoint = new EndpointConfiguration();
				endpoint.TypeInfo = typeof (EndpointMock).GetTypeInfo().AssemblyQualifiedName;
				port.Endpoint = endpoint;

				PipelineComponentConfiguration encoder = new PipelineComponentConfiguration();
				encoder.TypeInfo = typeof (PipelineComponentMock).GetTypeInfo().AssemblyQualifiedName;
				port.Encoders.Add(encoder);

				PipelineComponentConfiguration assembler = new PipelineComponentConfiguration();
				assembler.TypeInfo = typeof (PipelineComponentMock).GetTypeInfo().AssemblyQualifiedName;
				port.Assemblers.Add(assembler);
			}

			{
				// Send
				EndpointConfiguration endpoint = new EndpointConfiguration();
				var typeInfo = typeof (EndpointMock).GetTypeInfo();
				endpoint.TypeInfo = typeInfo.AssemblyQualifiedName;
				PortConfiguration port = new PortConfiguration();
				port.Endpoint = endpoint;
				config.Ports.Add(port);
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
					DictionaryProxy options = new DictionaryProxy(new Dictionary<string, string>());
					options.Add("property1", "value1&");
					options.Add("property2", "value2\n\nvalue2");
					options.Add("property3", "value3");
					filter.Options = options;
					source.Filters.Add(filter);
				}
				{
					TransformerConfiguration transformer = new TransformerConfiguration();
					var typeInfo = typeof (JavaScriptTransformer).GetTypeInfo();
					transformer.TypeInfo = typeInfo.AssemblyQualifiedName;
					DictionaryProxy options = new DictionaryProxy(new Dictionary<string, string>());
					options.Add("Script", "true");
					transformer.Options = options;

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

			Assert.IsNotNull(resultConfig.Ports);
			Assert.AreEqual(2, resultConfig.Ports.Count());
			Assert.IsNotNull(resultConfig.Ports.First().Endpoint);
			Assert.IsNotNull(resultConfig.Ports.First().Encoders);
			Assert.AreEqual(1, resultConfig.Ports.First().Encoders.Count());
			Assert.IsNotNull(resultConfig.Ports.First().Assemblers);
			Assert.AreEqual(1, resultConfig.Ports.First().Assemblers.Count());

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

			deserializedProperties = resultConfig.Channels.First().Source.Transformers.First().Options.ToDictionary();
			Assert.IsNotNull(deserializedProperties);
			Assert.AreEqual(1, deserializedProperties.Count());
			Assert.AreEqual("true", deserializedProperties["Script"]);
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
			Assert.IsNotNull(application.Ports);
			Assert.AreEqual(2, application.Ports.Count());
			Assert.AreEqual(typeof (EndpointMock), application.Ports.First().Endpoint.GetType());
			Assert.AreEqual(typeof (EndpointMock), application.Ports.Last().Endpoint.GetType());
			Assert.IsNotNull(application.Ports.First().Encoders);
			Assert.AreEqual(1, application.Ports.First().Encoders.Count());
			Assert.AreEqual(typeof (PipelineComponentMock), application.Ports.First().Encoders.First().GetType());
			Assert.IsNotNull(application.Ports.First().Assembers);
			Assert.AreEqual(1, application.Ports.First().Assembers.Count());
			Assert.AreEqual(typeof (PipelineComponentMock), application.Ports.First().Assembers.First().GetType());

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
			Assert.AreEqual(typeof (JavaScriptTransformer), application.Channels.First().Source.Transformers.First().GetType());

			JavaScriptTransformer transformer = (JavaScriptTransformer) application.Channels.First().Source.Transformers.First();

			Dictionary<string, string> properties = config.Channels.First().Source.Transformers.First().Options.ToDictionary();
			Assert.IsNotNull(properties);
			Assert.IsNotNull(properties, properties["Script"]);
			Assert.AreEqual(properties["Script"], transformer.Script);
		}
	}
}
