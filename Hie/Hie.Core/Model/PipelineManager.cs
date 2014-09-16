using System.Collections.Generic;
using System.Text;

namespace Hie.Core.Model
{
	public class PipelineManager : IPipelineManager
	{
		private readonly IApplicationHost _applicationHost;
		private Dictionary<IEndpoint, Queue<IPipelineComponent>> _pipelines = new Dictionary<IEndpoint, Queue<IPipelineComponent>>();

		public PipelineManager()
		{
		}

		public PipelineManager(IApplicationHost applicationHost)
		{
			_applicationHost = applicationHost;
		}

		public void AddPipelineComponent(IEndpoint endpoint, IPipelineComponent pipelineComponent)
		{
			Queue<IPipelineComponent> pipeline = null;

			if (_pipelines.ContainsKey(endpoint))
			{
				pipeline = _pipelines[endpoint];
			}
			else
			{
				pipeline = new Queue<IPipelineComponent>();
				_pipelines.Add(endpoint, pipeline);
			}

			pipeline.Enqueue(pipelineComponent);
		}

		public void PushPipelineData(IEndpoint endpoint, byte[] data)
		{
			// Find pipeline for endpoint and process..
			Queue<IPipelineComponent> pipeline;
			if (!_pipelines.TryGetValue(endpoint, out pipeline))
			{
				// Temporary. Remove during refactoring of endpoints.
				Message message = new Message("text/plain");
				message.Value = Encoding.UTF8.GetString(data);
				_applicationHost.PublishMessage(endpoint, message);
			}
			else
			{
				foreach (var component in pipeline)
				{
					var decoder = component as IDecoder;
					if (decoder != null)
					{
						data = decoder.Decode(data);
						continue;
					}

					var disassembler = component as IDisassembler;
					if (disassembler != null)
					{
						disassembler.Disassemble(data);

						Message message;
						do
						{
							message = disassembler.NextMessage();
							_applicationHost.PublishMessage(endpoint, message);
						} while (message != null);

						continue;
					}
				}
			}
		}
	}
}
