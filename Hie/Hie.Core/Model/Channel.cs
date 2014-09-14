using System.Collections.Generic;

namespace Hie.Core.Model
{
	public class Channel
	{
		public List<Destination> Destinations { get; private set; }
		public Source Source { get; set; }
		public ApplicationHost HostService { get; set; }

		public Channel()
		{
			Destinations = new List<Destination>();
		}
	}
}
