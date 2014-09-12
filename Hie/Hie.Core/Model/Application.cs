using System.Collections.Generic;
using System.Xml.Serialization;

namespace Hie.Core.Model
{
	public class Application
	{
		public List<Channel> Channels { get; private set; }
		public List<Endpoint> Endpoints { get; private set; }

		[XmlIgnore]
		public ApplicationHost HostService { get; set; }

		[XmlIgnore]
		public Dictionary<string, object> ApplicationMap { get; set; }

		public Application()
		{
			Endpoints = new List<Endpoint>();
			Channels = new List<Channel>();
			ApplicationMap = new Dictionary<string, object>();
		}


		public virtual void StartProcessing()
		{
		}
	}
}
