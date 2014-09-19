using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
			Stream = new MemoryStream();
		}

		public Message Clone()
		{
			Message clone = new Message(Schema);

			// Basic properties
			clone.Id = Guid.NewGuid();
			clone.CloneSource = Id;
			clone.CorrelationId = CorrelationId;
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

		public XDocument GetXDocument()
		{
			return RetrieveAs<XDocument>();
		}

		public XmlDocument GetXmlDocument()
		{
			return RetrieveAs<XmlDocument>();
		}

		public byte[] GetBytes()
		{
			return ((MemoryStream) GetStream()).ToArray();
		}

		public Stream GetStream(bool clone = false)
		{
			if (clone)
			{
				Stream.Position = 0;
				MemoryStream ms = new MemoryStream();
				Stream.CopyTo(ms);
				ms.Position = 0;
				return ms;
			}

			return RetrieveAs<Stream>();
		}

		public string GetString(Encoding encoding = null)
		{
			Stream.Position = 0;
			MemoryStream ms = new MemoryStream();
			Stream.CopyTo(ms);
			ms.Position = 0;
			if (encoding == null)
			{
				encoding = Encoding.UTF8;
			}

			return encoding.GetString(ms.ToArray());
		}

		public Message SetValueFrom(byte[] value)
		{
			Stream = new MemoryStream(value);
			return this;
		}

		public Message SetValueFrom(string value, Encoding encoding = null)
		{
			if (encoding == null) encoding = Encoding.UTF8;

			SetValueFrom(encoding.GetBytes(value));
			return this;
		}

		public Message SetValueFrom(XDocument document)
		{
			MemoryStream ms = new MemoryStream();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.OmitXmlDeclaration = true;
			settings.Encoding = new UTF8Encoding(false);
			settings.CloseOutput = true;

			XmlWriter writer = XmlWriter.Create(ms, settings);
			document.WriteTo(writer);
			writer.Flush();
			SetValueFrom(ms.ToArray());
			writer.Close();

			return this;
		}

		public Message SetValueFrom(XmlDocument document)
		{
			MemoryStream ms = new MemoryStream();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.OmitXmlDeclaration = true;
			settings.Encoding = new UTF8Encoding(false);

			XmlWriter writer = XmlTextWriter.Create(ms, settings);

			document.WriteTo(writer);
			SetValueFrom(ms.ToArray());
			writer.Close();
			return this;
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
