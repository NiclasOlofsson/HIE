namespace Hie.Core.Model
{
	public interface IDecoder : IPipelineComponent
	{
		byte[] Decode(byte[] data);
	}
}