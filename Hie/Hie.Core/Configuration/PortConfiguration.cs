using System.Collections.Generic;

namespace Hie.Core.Configuration
{
	public class PortConfiguration
	{
		public EndpointConfiguration Endpoint { get; set; }
		public List<PipelineComponentConfiguration> Encoders { get; set; }
		public List<PipelineComponentConfiguration> Assemblers { get; set; }

		public PortConfiguration()
		{
			Encoders = new List<PipelineComponentConfiguration>();
			Assemblers = new List<PipelineComponentConfiguration>();
		}
	}
}
