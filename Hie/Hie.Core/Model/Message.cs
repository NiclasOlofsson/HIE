using System;
using System.Collections.Generic;

namespace Hie.Core.Model
{
	public class Message : ICloneable
	{
		public Guid Id { get; private set; }
		public Guid CorrelationId { get; private set; }
		public string Value { get; set; }
		public Dictionary<string, object> MessageMap { get; set; }

		public Message()
		{
			Id = Guid.NewGuid();
			CorrelationId = Guid.NewGuid();
			MessageMap = new Dictionary<string, object>();
		}

		public Message Clone()
		{
			Message clone = (Message) MemberwiseClone();
			clone.Id = Guid.NewGuid();
			clone.MessageMap = new Dictionary<string, object>(MessageMap);
			return clone;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
