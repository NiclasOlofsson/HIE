namespace Hie.Core.Model
{
	public class DelegateFilter : IFilter
	{
		public delegate bool FilterProcessor(Message message);

		private FilterProcessor _processor;

		public DelegateFilter(FilterProcessor processor)
		{
			_processor = processor;
		}

		public bool Evaluate(Message message)
		{
			return _processor(message);
		}
	}
}
