using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using Hie.Core.Model;

namespace Hie.Core.Configuration
{
	public class ApplicationConfiguration
	{
		public string Name { get; set; }

		public string Description { get; set; }
		public List<PortConfiguration> Ports { get; private set; }
		public List<ChannelConfiguration> Channels { get; private set; }


		public ApplicationConfiguration()
		{
			Channels = new List<ChannelConfiguration>();
			Ports = new List<PortConfiguration>();
		}

		public Application CreateApplication()
		{
			Application application = new Application { Name = Name, Description = Description };

			foreach (var portConfig in Ports)
			{
				Port port = new Port();
				IEndpoint endpoint = CreateInstanace(portConfig.Endpoint.TypeInfo) as IEndpoint;
				port.Endpoint = endpoint;

				foreach (var encoderConfig in portConfig.Encoders)
				{
					var encoder = CreateInstanace(encoderConfig.TypeInfo) as IPipelineComponent;
					port.Encoders.Add(encoder);
				}

				foreach (var assemblerConfig in portConfig.Assemblers)
				{
					var encoder = CreateInstanace(assemblerConfig.TypeInfo) as IPipelineComponent;
					port.Assembers.Add(encoder);
				}

				application.Ports.Add(port);
			}

			foreach (var channelConfig in Channels)
			{
				Channel channel = new Channel();
				application.Channels.Add(channel);

				Source source = new Source();
				channel.Source = source;
				LoadFilters(channelConfig.Source.Filters, source.Filters);
				LoadTransformers(channelConfig.Source.Transformers, source.Transformers);

				foreach (var destinationConfiguration in channelConfig.Destinations)
				{
					var destination = new Destination();
					channel.Destinations.Add(destination);

					LoadFilters(destinationConfiguration.Filters, destination.Filters);
					LoadTransformers(destinationConfiguration.Transformers, destination.Transformers);
				}
			}

			return application;
		}

		private static void LoadFilters(IEnumerable<FilterConfiguration> filterConfigurations, List<IFilter> filters)
		{
			foreach (var filterConfiguration in filterConfigurations)
			{
				IFilter filter = CreateInstanace(filterConfiguration.TypeInfo) as IFilter;
				SetProperties(filter, filterConfiguration.Options);
				filters.Add(filter);
			}
		}

		private static void LoadTransformers(IEnumerable<TransformerConfiguration> transformerConfigurations, List<ITransformer> transformers)
		{
			foreach (var transformerConfiguration in transformerConfigurations)
			{
				ITransformer transformer = CreateInstanace(transformerConfiguration.TypeInfo) as ITransformer;
				SetProperties(transformer, transformerConfiguration.Options);
				transformers.Add(transformer);
			}
		}

		private static void SetProperties(object target, DictionaryProxy options)
		{
			if (options == null) return;

			foreach (KeyValuePair<string, string> item in options.ToDictionary())
			{
				PropertyInfo property = target.GetType().GetProperty(item.Key, typeof (string));
				if (property != null) property.SetValue(target, item.Value);
				//else throw new Exception("Type: " + target.GetType().Name + " Property: " + item.Key);
			}
		}

		private static object CreateInstanace(string assemblyQualifiedName)
		{
			Type type = Type.GetType(assemblyQualifiedName);
			return Activator.CreateInstance(type);
		}

		public static ApplicationConfiguration Load(string filePath)
		{
			ApplicationConfiguration config = null;

			XmlSerializer serializer = new XmlSerializer(typeof (ApplicationConfiguration));
			using (TextReader sr = new StreamReader(filePath))
			{
				config = (ApplicationConfiguration) serializer.Deserialize(sr);
			}

			return config;
		}

		public void Save(string filePath)
		{
			XmlSerializer serializer = new XmlSerializer(typeof (ApplicationConfiguration));

			using (TextWriter writer = new StreamWriter(filePath))
			{
				serializer.Serialize(writer, this);
			}
		}

		public void SaveSchema(string filePath)
		{
			XmlSerializer serializer = new XmlSerializer(typeof (ApplicationConfiguration));

			using (TextWriter writer = new StreamWriter(filePath))
			{
				serializer.Serialize(writer, this);
			}


			XmlSchemas schemas = new XmlSchemas();
			XmlSchemaExporter exporter = new XmlSchemaExporter(schemas);

			//Import the type as an XML mapping
			XmlTypeMapping mapping = new XmlReflectionImporter().ImportTypeMapping(typeof (ApplicationConfiguration));

			//Export the XML mapping into schemas
			exporter.ExportTypeMapping(mapping);

			using (TextWriter writer = new StreamWriter(filePath))
			{
				foreach (object schema in schemas)
				{
					((XmlSchema) schema).Write(writer);
					writer.WriteLine();
				}
			}
		}
	}
}
