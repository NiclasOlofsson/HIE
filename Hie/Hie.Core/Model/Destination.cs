using System.Collections.Generic;

namespace Hie.Core.Model
{
	public class Destination
	{
		public Endpoint Target { get; set; }
		public List<Filter> Filters { get; private set; }
		public List<Transformer> Transformers { get; private set; }
		public Channel Channel { get; set; }
		public Dictionary<string, object> DestinationMap { get; set; }

		public Destination()
		{
			Transformers = new List<Transformer>();
			Filters = new List<Filter>();
			DestinationMap = new Dictionary<string, object>();
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
			Channel.HostService.RouteMessage(this, Target, message);
		}
	}
}
