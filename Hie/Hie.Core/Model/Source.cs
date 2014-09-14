using System.Collections.Generic;
using System.Xml.Serialization;

namespace Hie.Core.Model
{
	public class Source
	{
		public Channel Channel { get; set; }
		public List<IFilter> Filters { get; private set; }
		public List<ITransformer> Transformers { get; private set; }

		[XmlIgnore]
		public Dictionary<string, object> SourceMap { get; set; }

		public Source()
		{
			Filters = new List<IFilter>();
			Transformers = new List<ITransformer>();
			SourceMap = new Dictionary<string, object>();
		}

		//TODO: Implement this as async
		public virtual bool AcceptMessage(object source, Message message)
		{
			// Apply filters
			bool accept = true;
			foreach (var filter in Filters)
			{
				accept &= filter.Evaluate(message);
				if (!accept) return false;
			}

			return true;
		}

		//TODO: Implement this as async
		public virtual void ProcessMessage(object source, Message message)
		{
			// Apply transformers
			foreach (var transformer in Transformers)
			{
				transformer.ProcessMessage(message);
			}

			// Route to target
			Channel.HostService.PublishMessage(this, Channel, message);
		}
	}
}
