using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Schema;
using System.Xml.Serialization;
using Hie.Core.Model;

namespace Hie.Core.Configuration
{
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

		public Application CreateApplication()
		{
			Application application = new Application();
			application.Name = Name;
			application.Description = Description;

			foreach (var endpointConfig in Endpoints)
			{
				IEndpoint endpoint = CreateInstanace(endpointConfig.TypeInfo) as IEndpoint;
				application.Endpoints.Add(endpoint);
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
			foreach (var filterConfig in filterConfigurations)
			{
				IFilter filter = CreateInstanace(filterConfig.TypeInfo) as IFilter;
				filters.Add(filter);
			}
		}

		private static void LoadTransformers(IEnumerable<TransformerConfiguration> transformerConfigurations, List<ITransformer> transformers)
		{
			foreach (var filterConfig in transformerConfigurations)
			{
				ITransformer transformer = CreateInstanace(filterConfig.TypeInfo) as ITransformer;
				transformers.Add(transformer);
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
