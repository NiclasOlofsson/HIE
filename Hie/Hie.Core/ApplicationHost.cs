using System;
using System.Collections.Generic;
using Hie.Core.Model;

namespace Hie.Core
{
	public class ApplicationHost
	{
		public IList<Application> Applications { get; set; }

		public ApplicationHost()
		{
			Applications = new List<Application>();
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

		public void RouteMessage(object source, object target, Message message)
		{
			if (source is Endpoint && target is Channel)
			{
				((Channel) target).Source.ProcessMessage(source, message.Clone());
			}
			else if (source is Source)
			{
				foreach (Destination destination in ((Channel) target).Destinations)
				{
					destination.ProcessMessage(source, message.Clone());
				}
			}
			else if (source is Destination && target is Endpoint)
			{
				((Endpoint) target).ProcessMessage(source, message.Clone());
			}
			else
			{
				throw new Exception(string.Format("Illegal route. Source: {0}, Target {1}, Message {2}", source, target, message.Id));
			}
		}

		public void BroadcastMessage(object source, Message message)
		{
			if (source is Endpoint)
			{
				foreach (Application application in Applications)
				{
					foreach (var channel in application.Channels)
					{
						channel.Source.ProcessMessage(source, message.Clone());
					}
				}
			}
			else
			{
				throw new Exception(string.Format("Illegal broadcast source. Source: {0}, Message {1}", source, message.Id));
			}
		}
	}
}
