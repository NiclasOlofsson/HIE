using System.Collections.Generic;
using System.Xml.Serialization;

namespace Hie.Core.Model
{
	public class Channel
	{
		public List<Destination> Destinations { get; private set; }
		public Source Source { get; set; }

		[XmlIgnore]
		public ApplicationHost HostService { get; set; }

		[XmlIgnore]
		public Dictionary<string, object> ChannelMap { get; set; }

		public Channel()
		{
			Destinations = new List<Destination>();
			ChannelMap = new Dictionary<string, object>();
		}
	}
}
