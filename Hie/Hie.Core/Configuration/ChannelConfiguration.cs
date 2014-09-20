using System.Collections.Generic;

namespace Hie.Core.Configuration
{
	public class ChannelConfiguration
	{
		public string Name { get; set; }

		public string Description { get; set; }
		public SourceConfiguration Source { get; set; }
		public List<DestinationConfiguration> Destinations { get; set; }

		public ChannelConfiguration()
		{
			Destinations = new List<DestinationConfiguration>();
		}
	}
}