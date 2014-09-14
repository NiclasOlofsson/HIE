namespace Hie.Core.Model
{
	public class DelegateTransformer : ITransformer
	{
		public delegate void TransformerProcessor(object source, Message message);

		private TransformerProcessor _processor;

		public DelegateTransformer()
		{
		}

		public DelegateTransformer(TransformerProcessor processor)
		{
			_processor = processor;
		}

		public void ProcessMessage(object source, Message message)
		{
			if (_processor != null) _processor(source, message);
		}
	}
}