using System.Text;
using Hie.Core.Mocks;
using Hie.Core.Model;
using Hie.Core.Modules.JavaScript;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core
{
	[TestClass]
	public class HieEngineTest
	{
		[TestMethod]
		public void BasicRoutingFilteringTransformationTest()
		{
			// A new application
			Application application = new Application();

			// Ports
			Port receivePort = new Port();
			IEndpoint endpoint = new EndpointMock();
			receivePort.Endpoint = endpoint;
			{
				IEncoder encoder = new PipelineComponentMock();
				receivePort.Encoders.Add(encoder);
				IDisassembler disassembler = new PipelineComponentMock();
				receivePort.Assembers.Add(disassembler);
			}
			application.Ports.Add(receivePort);

			Port sendPort = new Port();
			IEndpoint sendEndpoint = new EndpointMock();
			sendPort.Endpoint = sendEndpoint;
			{
				IEncoder encoder = new PipelineComponentMock();
				sendPort.Encoders.Add(encoder);
				IDisassembler disassembler = new PipelineComponentMock();
				sendPort.Assembers.Add(disassembler);
			}
			application.Ports.Add(sendPort);

			// Add a channel
			Channel channel = new Channel();
			application.Channels.Add(channel);

			// Source setup
			Source source = new Source();
			channel.Source = source;
			source.Filters.Add(new DelegateFilter((src, message) => true));
			source.Filters.Add(new JavaScriptFilter { Script = "true" });
			source.Transformers.Add(new DelegateTransformer());
			source.Transformers.Add(new DelegateTransformer((src, message) => { }));
			source.Transformers.Add(new DelegateTransformer((src, message) => message.SetValueFrom(message.GetString())));

			{
				Destination destination = new Destination();
				destination.Target = sendEndpoint;
				destination.Filters.Add(new DelegateFilter((src, message) => true));
				destination.Filters.Add(new JavaScriptFilter { Script = "true" });
				destination.Transformers.Add(new DelegateTransformer((src, message) => { }));
				destination.Transformers.Add(new DelegateTransformer((src, message) => message.SetValueFrom(message.GetString())));
				channel.Destinations.Add(destination);
			}
			{
				// This destination will filter out the message
				Destination destination = new Destination();
				destination.Target = sendEndpoint;
				destination.Filters.Add(new DelegateFilter((src, message) => false));
				channel.Destinations.Add(destination);
			}

			{
				// This destination will transform the message
				Destination destination = new Destination();
				destination.Target = sendEndpoint;
				destination.Filters.Add(new DelegateFilter((src, message) => true));
				destination.Transformers.Add(new DelegateTransformer((src, message) => message.SetValueFrom(message.GetString() + "test")));
				channel.Destinations.Add(destination);
			}

			// Host
			ApplicationHost applicationHost = new ApplicationHost();
			Assert.IsNotNull(applicationHost.Applications);
			applicationHost.Deploy(application);

			// Start the processing

			Message testMessage = new Message("text/json");
			testMessage.SetValueFrom("AAAA");
			// Mock method for sending a test message
			((EndpointMock) endpoint).SendTestMessage(testMessage);

			// Check that endpoint received the message
			EndpointMock endpointMock = sendEndpoint as EndpointMock;
			Assert.IsNotNull(endpointMock);
			Assert.IsNotNull(endpointMock.Messages);
			Assert.AreEqual(2, endpointMock.Messages.Count);
			foreach (byte[] data in endpointMock.Messages)
			{
				string actual = Encoding.UTF8.GetString(data);
				if (actual.EndsWith("test"))
				{
					Assert.AreEqual(testMessage.GetString() + "test", actual);
				}
				else
				{
					Assert.AreEqual(testMessage.GetString(), actual);
				}
			}
		}
	}
}
