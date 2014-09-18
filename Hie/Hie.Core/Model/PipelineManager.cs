using System.Collections.Generic;
using System.Linq;
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
			Queue<IPipelineComponent> pipeline;

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
				byte[] decoded = null;
				foreach (IDecoder component in pipeline.OfType<IDecoder>())
				{
					decoded = component.Decode(data);
					if (decoded != null) break;
				}

				if (decoded == null) return;

				foreach (IDisassembler component in pipeline.OfType<IDisassembler>())
				{
					component.Disassemble(decoded);

					Message message;
					do
					{
						message = component.NextMessage();
						_applicationHost.PublishMessage(endpoint, message);
					} while (message != null);
				}
			}
		}

		public void PushPipelineData(IEndpoint endpoint, Message message)
		{
			Queue<IPipelineComponent> pipeline;
			if (!_pipelines.TryGetValue(endpoint, out pipeline))
			{
				//TODO: Temporary for testing
				var data = Encoding.UTF8.GetBytes(message.Value);
				endpoint.ProcessMessage(endpoint, data);
			}
			else
			{
				byte[] data = null;
				foreach (IAssembler component in pipeline.OfType<IAssembler>())
				{
					component.AddMessage(message);
					data = component.Assemble();

					// Decide what to do if data is returned.
					// Right now, we break out and go to encoders
					if (data != null) break;
				}

				if (data == null) return;

				foreach (IEncoder component in pipeline.OfType<IEncoder>())
				{
					data = component.Encode(data);

					if (data != null)
					{
						endpoint.ProcessMessage(endpoint, data);
					}
				}
			}
		}
	}
}
