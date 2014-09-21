using System.Collections.Generic;

namespace Hie.Core.Model
{
	public class Application
	{
		public string Name { get; set; }
		public string Description { get; set; }

		public List<Channel> Channels { get; private set; }
		public Dictionary<string, object> ApplicationMap { get; set; }
		public List<Port> Ports { get; set; }

		public ApplicationHost HostService { get; set; }

		public Application()
		{
			Channels = new List<Channel>();
			ApplicationMap = new Dictionary<string, object>();
			Ports = new List<Port>();
		}


		public virtual void StartProcessing()
		{
		}
	}
}
