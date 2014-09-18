namespace Hie.Core.Model
{
	public interface IEncoder : IPipelineComponent
	{
		byte[] Encode(byte[] data);
	}
}
