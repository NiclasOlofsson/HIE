using System.Net;
using System.Text;
using System.Xml.Linq;
using Hie.Core;
using Hie.Core.Endpoints;
using Hie.Core.Model;
using Hie.Core.Modules.JavaScript;
using Topshelf;

namespace Hie.Service
{
	public class HieEngine
	{
		private ApplicationHost _applicationHost;

		public HieEngine()
		{
			// The HIE Applicatoin Host

			_applicationHost = new ApplicationHost();

			Application application = CreateHl7Application(_applicationHost);

			_applicationHost.Deploy(application);
		}

		private Application CreateHl7Application(ApplicationHost pipelineManager)
		{
			Application application = new Application();

			// Endpoints

			{
				var receiveEndoint = new TcpReceiveEndpoint();
				TcpReceieveOptions options = new TcpReceieveOptions { Endpoint = new IPEndPoint(IPAddress.Any, 5678) };
				options.SohDelimiters = new byte[0];
				options.EotDelimiters = new byte[0];
				receiveEndoint.Initialize(_applicationHost, options);
				Port port = new Port { Endpoint = receiveEndoint };
				port.Assembers.Add(new Hl7Disassembler());
				application.Ports.Add(port);
			}
			{
				var sendEndpoint = new TcpSendEndpoint();
				TcpSendOptions options = new TcpSendOptions() { Endpoint = new IPEndPoint(IPAddress.Loopback, 8756) };
				options.SohDelimiters = new byte[0];
				options.EotDelimiters = new byte[0];
				sendEndpoint.Initialize(_applicationHost, options);
				Port port = new Port { Endpoint = sendEndpoint };
				port.Assembers.Add(new Hl7Assembler());
				application.Ports.Add(port);
			}
			{
				var fileEndpoint = new FileWriterEndpoint("service-output.txt", true, Encoding.UTF8, false);
				fileEndpoint.Initialize(_applicationHost, null);
				Port port = new Port { Endpoint = fileEndpoint };
				application.Ports.Add(port);
			}

			// Channels

			Channel channel = new Channel();
			channel.Source = new Source();
			application.Channels.Add(channel);

			{
				// This destination will transform the message
				Destination destination = new Destination();
				destination.Filters.Add(new DelegateFilter((src, message) => true));
				destination.Transformers.Add(new DelegateTransformer(TransformerTest));
				destination.Transformers.Add(new JavaScriptTransformer()
				{
					Script = @"

if(msg['MSH']['MSH.8'] != null) delete msg['MSH']['MSH.8'];
msg['MSH']['MSH.2'] = 'TEST';

"
				});
				channel.Destinations.Add(destination);
			}

			return application;
		}

		private void TransformerTest(object source, Message message)
		{
			XDocument doc = message.GetXDocument();
			doc.Descendants("MSH.11").Remove();
			message.SetValueFrom(doc);
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
