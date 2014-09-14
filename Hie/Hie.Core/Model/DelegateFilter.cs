namespace Hie.Core.Model
{
	public class DelegateFilter : IFilter
	{
		public delegate bool FilterProcessor(object source, Message message);

		private FilterProcessor _processor;

		public DelegateFilter(FilterProcessor processor)
		{
			_processor = processor;
		}

		public bool Evaluate(object source, Message message)
		{
			return _processor(source, message);
		}
	}
}
