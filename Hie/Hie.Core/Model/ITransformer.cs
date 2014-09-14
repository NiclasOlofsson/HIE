namespace Hie.Core.Model
{
	public interface ITransformer
	{
		void ProcessMessage(object source, Message message);
	}
}
