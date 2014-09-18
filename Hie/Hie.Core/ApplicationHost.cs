using System;
using System.Collections.Generic;
using Hie.Core.Model;

namespace Hie.Core
{
	public interface IApplicationHost
	{
		void Deploy(Application application);
		void StartProcessing();
		void StopProcessing();
		void PublishMessage(object source, Message message);
		void ProcessInPipeline(IEndpoint source, byte[] data);
		void ProcessInPipeline(IEndpoint source, Message message);
	}

	public class ApplicationHost : IApplicationHost
	{
		private IPipelineManager _pipelineManager;
		public IList<Application> Applications { get; set; }

		public ApplicationHost(IPipelineManager pipelineManager = null)
		{
			Applications = new List<Application>();
			if (pipelineManager == null)
			{
				_pipelineManager = new PipelineManager(this);
			}
			else
			{
				_pipelineManager = pipelineManager;
			}
		}

		public void Deploy(Application application)
		{
			// Setup application
			application.HostService = this;

			// Setup channels
			foreach (var channel in application.Channels)
			{
				channel.HostService = this;
				foreach (var destination in channel.Destinations)
				{
					destination.Channel = channel;
				}
				channel.Source.Channel = channel;
			}

			// Setup endpoints
			foreach (var endpoint in application.Endpoints)
			{
				endpoint.HostService = this;
			}

			Applications.Add(application);
		}

		public void StartProcessing()
		{
			foreach (var application in Applications)
			{
				foreach (var endpoint in application.Endpoints)
				{
					endpoint.StartProcessing();
				}
			}
		}

		public void StopProcessing()
		{
			foreach (var application in Applications)
			{
				foreach (var endpoint in application.Endpoints)
				{
					endpoint.StopProcessing();
				}
			}
		}

		public void PublishMessage(object source, Message message)
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
					foreach (var endpoint in application.Endpoints)
					{
						ProcessInPipeline(endpoint, message);
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
			_pipelineManager.PushPipelineData(source, data);
		}

		public void ProcessInPipeline(IEndpoint source, Message message)
		{
			_pipelineManager.PushPipelineData(source, message);
		}
	}
}
