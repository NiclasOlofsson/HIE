using System;
using System.Net;
using System.Text;
using Hie.Core;
using Hie.Core.Endpoints;
using Hie.Core.Model;
using Topshelf;

namespace Hie.Service
{
	public class HieEngine
	{
		private ApplicationHost _applicationHost = new ApplicationHost();

		public HieEngine()
		{
			// The HIE Applicatoin Host

			Application application = CreateApplication();

			_applicationHost.Deploy(application);
		}

		private Application CreateApplication()
		{
			Application application = new Application();

			{
				var receiveEndoint = new TcpReceiveEndpoint();
				receiveEndoint.Initialize(new TcpReceieveOptions { Endpoint = new IPEndPoint(IPAddress.Any, 5678) });
				application.Endpoints.Add(receiveEndoint);
			}
			{
				var sendEndpoint = new TcpSendEndpoint();
				sendEndpoint.Initialize(new TcpSendOptions() { Endpoint = new IPEndPoint(IPAddress.Loopback, 8756) });
				application.Endpoints.Add(sendEndpoint);
			}
			{
				var fileEndpoint = new FileWriterEndpoint("service-output.txt", true, Encoding.UTF8, false);
				fileEndpoint.Initialize(new TcpSendOptions() { Endpoint = new IPEndPoint(IPAddress.Loopback, 8756) });
				application.Endpoints.Add(fileEndpoint);
			}

			// Add a channel
			Channel channel = new Channel();
			application.Channels.Add(channel);

			// Source setup
			Source source = new Source();
			channel.Source = source;


			{
				// This destination will transform the message
				Destination destination = new Destination();
				destination.Filters.Add(new DelegateFilter((src, message) => true));
				destination.Transformers.Add(new DelegateTransformer((src, message) => { message.Value = string.Format("[{0}] Message transformed: {{{1}}}\n\n", DateTime.Now, message.Value); }));
				channel.Destinations.Add(destination);
			}

			return application;
		}

		public void Start()
		{
			_applicationHost.StartProcessing();
		}

		public void Stop()
		{
			_applicationHost.StopProcessing();
		}
	}

	public class Program
	{
		private static void Main(string[] args)
		{
			HostFactory.Run(host =>
			{
				host.Service<HieEngine>(s =>
				{
					s.ConstructUsing(construct => new HieEngine());
					s.WhenStarted(service => service.Start());
					s.WhenStopped(service => service.Stop());
				});

				host.RunAsLocalService();
				host.SetDisplayName("HIE");
				host.SetDescription("Healthcare Integration Engine Service");
				host.SetServiceName("HIE");
			});
		}
	}
}
