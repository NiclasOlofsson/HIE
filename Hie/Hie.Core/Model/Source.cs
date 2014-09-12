using System.Collections.Generic;
using System.Xml.Serialization;

namespace Hie.Core.Model
{
	public class Source
	{
		public Channel Channel { get; set; }
		public List<Filter> Filters { get; private set; }
		public List<Transformer> Transformers { get; private set; }

		[XmlIgnore]
		public Dictionary<string, object> SourceMap { get; set; }

		public Source()
		{
			Filters = new List<Filter>();
			Transformers = new List<Transformer>();
			SourceMap = new Dictionary<string, object>();
		}

		public virtual void ProcessMessage(object source, Message message)
		{
			// Apply filters
			bool accept = true;
			foreach (var filter in Filters)
			{
				accept &= filter.Evaluate(message);
				if (!accept) return;
			}

			// Apply transformers
			foreach (var transformer in Transformers)
			{
				transformer.ProcessMessage(message);
			}

			// Route to target
			Channel.HostService.RouteMessage(this, Channel, message);
		}
	}
}
