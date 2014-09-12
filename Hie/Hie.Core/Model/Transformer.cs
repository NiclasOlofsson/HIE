namespace Hie.Core.Model
{
	public class Transformer
	{
		public Transformer()
		{
		}

		public virtual void ProcessMessage(Message message)
		{
		}
	}

	public class DelegateTransformer : Transformer
	{
		public delegate void TransformerProcessor(Message message);

		private TransformerProcessor _processor;

		public DelegateTransformer()
		{
		}

		public DelegateTransformer(TransformerProcessor processor)
		{
			_processor = processor;
		}

		public override void ProcessMessage(Message message)
		{
			_processor(message);
		}
	}
}
