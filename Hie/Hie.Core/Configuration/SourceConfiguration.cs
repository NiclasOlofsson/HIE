using System.Collections.Generic;

namespace Hie.Core.Configuration
{
	public class SourceConfiguration
	{
		public List<FilterConfiguration> Filters { get; private set; }
		public List<TransformerConfiguration> Transformers { get; private set; }

		public SourceConfiguration()
		{
			Filters = new List<FilterConfiguration>();
			Transformers = new List<TransformerConfiguration>();
		}
	}
}