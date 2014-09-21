using System;
using System.Collections.Generic;
using System.Linq;
using Hie.Core.Model;

namespace Hie.Core
{
	public interface IApplicationHost
	{
		void Deploy(Application app);
		void StartProcessing();
		void StopProcessing();

		void PublishMessage(object source, Message message);

		void AddPipelineComponent(IEndpoint endpoint, IPipelineComponent pipelineComponent);
		void PushPipelineData(IEndpoint endpoint, byte[] data);
		void PushPipelineData(IEndpoint endpoint, Message message);

		void ProcessInPipeline(IEndpoint source, byte[] data);
		void ProcessInPipeline(IEndpoint source, Message message);
	}

	public class ApplicationHost : IApplicationHost
	{
		private Dictionary<IEndpoint, Queue<IPipelineComponent>> _pipelines = new Dictionary<IEndpoint, Queue<IPipelineComponent>>();

		public IList<Application> Applications { get; private set; }

		public ApplicationHost()
		{
			Applications = new List<Application>();
		}

		public void Deploy(Application app)
		{
			// Setup application
			app.HostService = this;

			foreach (Port port in app.Ports)
			{
				foreach (var encoder in port.Encoders)
				{
					AddPipelineComponent(port.Endpoint, encoder);
				}

				foreach (var assembler in port.Assembers)
				{
					AddPipelineComponent(port.Endpoint, assembler);
				}
			}

			// Setup channels
			foreach (var channel in app.Channels)
			{
				channel.HostService = this;
				foreach (var destination in channel.Destinations)
				{
					destination.Channel = channel;
				}
				channel.Source.Channel = channel;
			}

			// Setup endpoints
			foreach (var port in app.Ports)
			{
				port.Endpoint.Initialize(this, null);
			}

			Applications.Add(app);
		}

		public void StartProcessing()
		{
			foreach (var application in Applications)
			{
				foreach (var port in application.Ports)
				{
					port.Endpoint.StartProcessing();
				}
			}
		}

		public void StopProcessing()
		{
			foreach (var application in Applications)
			{
				foreach (var port in application.Ports)
				{
					port.Endpoint.StopProcessing();
				}
			}
		}

		public virtual void PublishMessage(object source, Message message)
		{
			// Store message in queue (message box). Not yet implemented, but is what publish/subscribe will do

			// Process messages

			if (source is IEndpoint)
			{
				foreach (var application in Applications)
				{
					foreach (var channel in application.Channels)
					{
						if (channel.Source.AcceptMessage(source, message))
						{
							channel.Source.ProcessMessage(source, message.Clone());
						}
					}
				}
			}
			else if (source is Source)
			{
				// This is coming from source after transformation
				foreach (Destination destination in ((Source) source).Channel.Destinations)
				{
					if (destination.AcceptMessage((Source) source, message))
					{
						destination.ProcessMessage((Source) source, message.Clone());
					}
				}
			}
			else if (source is Destination)
			{
				foreach (var application in Applications)
				{
					foreach (var port in application.Ports)
					{
						ProcessInPipeline(port.Endpoint, message);
					}
				}
			}
			else
			{
				throw new Exception(string.Format("Illegal route. Source: {0}, Message {2}", source, message.Id));
			}
		}

		public void ProcessInPipeline(IEndpoint source, byte[] data)
		{
			PushPipelineData(source, data);
		}

		public void ProcessInPipeline(IEndpoint source, Message message)
		{
			PushPipelineData(source, message);
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
				message.SetValueFrom(data);
				PublishMessage(endpoint, message);
			}
			else
			{
				byte[] decoded = null;
				foreach (IDecoder component in pipeline.OfType<IDecoder>())
				{
					decoded = component.Decode(data);
					if (decoded != null) break;
				}

				if (decoded == null) decoded = data;

				foreach (IDisassembler component in pipeline.OfType<IDisassembler>())
				{
					component.Disassemble(decoded);

					Message message;
					do
					{
						message = component.NextMessage();
						if (message != null) PublishMessage(endpoint, message);
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
				endpoint.ProcessMessage(endpoint, message.GetBytes());
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

				if (data == null) data = message.GetBytes();

				foreach (IEncoder component in pipeline.OfType<IEncoder>())
				{
					data = component.Encode(data);
				}

				if (data != null)
				{
					endpoint.ProcessMessage(endpoint, data);
				}
			}
		}
	}
}
