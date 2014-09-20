using System.Collections.Generic;

namespace Hie.Core.Configuration
{
	public class DestinationConfiguration
	{
		public List<FilterConfiguration> Filters { get; private set; }
		public List<TransformerConfiguration> Transformers { get; private set; }

		public DestinationConfiguration()
		{
			Filters = new List<FilterConfiguration>();
			Transformers = new List<TransformerConfiguration>();
		}
	}
}