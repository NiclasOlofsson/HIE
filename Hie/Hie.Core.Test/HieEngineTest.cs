using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Hie.Core.Endpoints;
using Hie.Core.Model;
using Hie.Core.Modules.JavaScript;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core.Test
{
	[TestClass]
	public class HieEngineTest
	{
		[TestMethod]
		public void BasicRoutingFilteringTransformationTest()
		{
			// A new application
			Application application = new Application();

			// Add endpoints
			IEndpoint endpoint = new MockEndpoint(EndpointDirection.OneWayReceive);
			application.Endpoints.Add(endpoint);

			IEndpoint sendEndpoint = new MockEndpoint(EndpointDirection.OneWaySend);
			application.Endpoints.Add(sendEndpoint);

			// Add a channel
			Channel channel = new Channel();
			application.Channels.Add(channel);

			// Source setup
			Source source = new Source();
			channel.Source = source;
			source.Filters.Add(new DelegateFilter((src, message) => true));
			source.Filters.Add(new JavaScriptFilter {Script = "true"});
			source.Transformers.Add(new DelegateTransformer());
			source.Transformers.Add(new DelegateTransformer((src, message) => { }));
			source.Transformers.Add(new DelegateTransformer((src, message) => { message.Value = message.Value; }));

			{
				Destination destination = new Destination();
				destination.Target = sendEndpoint;
				destination.Filters.Add(new DelegateFilter((src, message) => true));
				destination.Filters.Add(new JavaScriptFilter {Script = "true"});
				destination.Transformers.Add(new DelegateTransformer((src, message) => { }));
				destination.Transformers.Add(new DelegateTransformer((src, message) => { message.Value = message.Value; }));
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
				destination.Transformers.Add(new DelegateTransformer((src, message) => { message.Value = message.Value + "test"; }));
				channel.Destinations.Add(destination);
			}

			// Host
			ApplicationHost applicationHost = new ApplicationHost();
			Assert.IsNotNull(applicationHost.Applications);
			applicationHost.Deploy(application);

			// Start the processing

			Message testMessage = new Message("text/json") {Value = TestUtils.BuildHl7JsonString()};
			// Mock method for sending a test message
			((MockEndpoint) endpoint).SendTestMessage(testMessage);

			// Check that endpoint received the message
			MockEndpoint mockEndpoint = sendEndpoint as MockEndpoint;
			Assert.IsNotNull(mockEndpoint);
			Assert.IsNotNull(mockEndpoint.Messages);
			Assert.AreEqual(2, mockEndpoint.Messages.Count);
			foreach (Message message in mockEndpoint.Messages)
			{
				Assert.AreNotSame(testMessage, message);
				Assert.AreNotEqual(testMessage.Id, message.Id);
				if (message.Value.EndsWith("test"))
				{
					Assert.AreEqual(testMessage.Value + "test", message.Value);
				}
				else
				{
					Assert.AreEqual(testMessage.Value, message.Value);
				}
			}
		}

		[TestMethod]
		public void BasicTcpEndpointTest()
		{
			// A new application
			Application application = new Application();

			// Add endpoints
			TcpReceiveEndpoint receiveEndpoint = new TcpReceiveEndpoint(new IPEndPoint(IPAddress.Any, 6789));
			application.Endpoints.Add(receiveEndpoint);

			IEndpoint sendEndpoint = new MockEndpoint(EndpointDirection.OneWaySend);
			application.Endpoints.Add(sendEndpoint);

			// Add a channel
			Channel channel = new Channel();
			application.Channels.Add(channel);
			receiveEndpoint.DirectTarget = channel;

			// Source setup
			Source source = new Source();
			channel.Source = source;

			Destination destination = new Destination();
			destination.Target = sendEndpoint;
			channel.Destinations.Add(destination);

			// Host
			ApplicationHost applicationHost = new ApplicationHost();
			applicationHost.Deploy(application);

			// Start the processing
			applicationHost.StartProcessing();


			{
				TcpClient client = new TcpClient();
				client.Connect(IPAddress.Loopback, 6789);
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.SOH, TcpReceiveEndpoint.STX, 0x41, 0x41, 0x41, 0x41, TcpReceiveEndpoint.ETX, TcpReceiveEndpoint.EOT}, 0, 8);
				receiveEndpoint.WaitForMessage();
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.SOH, TcpReceiveEndpoint.STX, 0x41, 0x41, 0x41, 0x41, TcpReceiveEndpoint.ETX, TcpReceiveEndpoint.EOT}, 0, 8);
				receiveEndpoint.WaitForMessage();
				client.Close();
			}
			{
				TcpClient client = new TcpClient();
				client.Connect(IPAddress.Loopback, 6789);
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.SOH, TcpReceiveEndpoint.STX, 0x41, 0x41, 0x41, 0x41, TcpReceiveEndpoint.ETX, TcpReceiveEndpoint.EOT}, 0, 8);
				receiveEndpoint.WaitForMessage();
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.SOH, TcpReceiveEndpoint.STX, 0x41, 0x41, 0x41, 0x41, TcpReceiveEndpoint.ETX, TcpReceiveEndpoint.EOT}, 0, 8);
				receiveEndpoint.WaitForMessage();
				client.Close();
			}

			// Check that endpoint received the message
			MockEndpoint mockEndpoint = sendEndpoint as MockEndpoint;
			Assert.IsNotNull(mockEndpoint);
			Assert.IsNotNull(mockEndpoint.Messages);
			Assert.AreEqual(3, mockEndpoint.Messages.Count);
			foreach (Message message in mockEndpoint.Messages)
			{
				Assert.AreEqual("AAAA", message.Value);
			}
		}


		[TestMethod]
		public void BasicFileEndpointTest()
		{
			// A new application
			Application application = new Application();

			// Add endpoints
			string filePath = "test-file.txt";
			FileReaderEndpoint fileReaderEndpoint = new FileReaderEndpoint(filePath, 100, Encoding.Default);
			application.Endpoints.Add(fileReaderEndpoint);

			string fileOutPath = "test-file-out.txt";
			FileWriterEndpoint fileWriterEndpoint = new FileWriterEndpoint(fileOutPath, true, Encoding.Default, true);
			application.Endpoints.Add(fileWriterEndpoint);

			// Add a channel
			Channel channel = new Channel();
			application.Channels.Add(channel);
			fileReaderEndpoint.DirectTarget = channel;

			// Source setup
			Source source = new Source();
			channel.Source = source;

			Destination destination = new Destination();
			destination.Target = fileWriterEndpoint;
			channel.Destinations.Add(destination);

			// Host
			ApplicationHost applicationHost = new ApplicationHost();
			applicationHost.Deploy(application);

			// Start the processing
			applicationHost.StartProcessing();
			fileReaderEndpoint.WaitForMessage();
			fileReaderEndpoint.WaitForMessage();

			// Check that endpoint wrote the message

			using (StreamReader reader = new StreamReader(fileOutPath))
			{
				string text = reader.ReadToEnd();
				Assert.AreEqual("Hello world!\nHello world!", text.Trim().Replace("\r\n", "\n"));
				reader.Close();
			}
		}
	}
}
