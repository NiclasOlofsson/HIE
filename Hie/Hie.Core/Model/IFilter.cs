namespace Hie.Core.Model
{
	public interface IFilter
	{
		bool Evaluate(object source, Message message);
	}
}
