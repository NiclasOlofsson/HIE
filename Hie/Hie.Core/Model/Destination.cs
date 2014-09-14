using System.Collections.Generic;

namespace Hie.Core.Model
{
	public class Destination
	{
		public IEndpoint Target { get; set; }
		public List<IFilter> Filters { get; private set; }
		public List<ITransformer> Transformers { get; private set; }
		public Channel Channel { get; set; }
		public Dictionary<string, object> DestinationMap { get; set; }

		public Destination()
		{
			Transformers = new List<ITransformer>();
			Filters = new List<IFilter>();
			DestinationMap = new Dictionary<string, object>();
		}


		public virtual void ProcessMessage(Source source, Message message)
		{
			// Apply transformers
			foreach (var transformer in Transformers)
			{
				transformer.ProcessMessage(message);
			}

			// Route to target
			Channel.HostService.PublishMessage(this, Target, message);
		}

		public bool AcceptMessage(Source source, Message message)
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
	}
}
