using System.Collections.Generic;

namespace Hie.Core.Model
{
	public class Port
	{
		public IEndpoint Endpoint { get; set; }
		//TODO: Split this in send/receive
		public List<IPipelineComponent> Encoders { get; private set; }
		public List<IPipelineComponent> Assembers { get; private set; }

		public Port()
		{
			Encoders = new List<IPipelineComponent>();
			Assembers = new List<IPipelineComponent>();
		}
	}
}
