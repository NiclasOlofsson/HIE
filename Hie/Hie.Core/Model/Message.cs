using System;
using System.Collections.Generic;

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

			// Maps
			clone.MessageMap = new Dictionary<string, object>(MessageMap);
			clone.PromotedProperties = new Dictionary<string, object>(PromotedProperties);

			return clone;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
