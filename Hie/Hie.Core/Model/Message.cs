using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Hie.Core.Model
{
	public class Message : ICloneable
	{
		public Guid Id { get; private set; }
		public Guid CloneSource { get; private set; }
		public Guid CorrelationId { get; private set; }
		public string Schema { get; private set; }
		public string Value { get; set; }

		public Dictionary<string, object> PromotedProperties { get; private set; }
		public Dictionary<string, object> MessageMap { get; private set; }
		public Stream Stream { get; set; }

		public Message(string schema)
		{
			Id = Guid.NewGuid();
			Schema = schema;
			CorrelationId = Guid.NewGuid();
			MessageMap = new Dictionary<string, object>();
			PromotedProperties = new Dictionary<string, object>();
		}

		public Message Clone()
		{
			Message clone = new Message(Schema);

			// Basic properties
			clone.Id = Guid.NewGuid();
			clone.CloneSource = Id;
			clone.CorrelationId = CorrelationId;
			clone.Value = Value;
			if (Stream != null)
			{
				clone.Stream = new MemoryStream();
				long pos = Stream.Position;
				Stream.CopyTo(clone.Stream);
				Stream.Position = pos;
				clone.Stream.Position = 0;
			}

			// Maps
			clone.MessageMap = new Dictionary<string, object>(MessageMap);
			clone.PromotedProperties = new Dictionary<string, object>(PromotedProperties);

			return clone;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public T RetrieveAs<T>() where T : class
		{
			if (typeof (T) == typeof (XDocument) || typeof (T) == typeof (XNode))
			{
				Stream.Position = 0;
				var doc = XDocument.Load(Stream) as T;
				Stream.Position = 0;
				return doc;
			}
			else if (typeof (T) == typeof (XmlDocument))
			{
				Stream.Position = 0;
				var doc = new XmlDocument();
				doc.Load(Stream);
				Stream.Position = 0;
				return doc as T;
			}
			else if (typeof (T) == typeof (Stream))
			{
				Stream.Position = 0;
				return Stream as T;
			}

			return null;
		}
	}
}
