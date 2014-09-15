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
			IEndpoint endpoint = new MockEndpoint();
			application.Endpoints.Add(endpoint);

			IEndpoint sendEndpoint = new MockEndpoint();
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

			IEndpoint sendEndpoint = new MockEndpoint();
			application.Endpoints.Add(sendEndpoint);

			IEndpoint sendEndpoint2 = new MockEndpoint();
			application.Endpoints.Add(sendEndpoint2);

			// Add a channel
			Channel channel = new Channel();
			application.Channels.Add(channel);
			receiveEndpoint.DirectTarget = channel;

			// Source setup
			Source source = new Source();
			channel.Source = source;

			Destination destination = new Destination();
			channel.Destinations.Add(destination);

			// Host
			ApplicationHost applicationHost = new ApplicationHost();
			applicationHost.Deploy(application);

			// Start the processing
			applicationHost.StartProcessing();


			{
				// Test default delimiters for SOH, STX, ETX and EOT

				var options = receiveEndpoint.GetOptions();
				options.SOHDelimiters = new byte[] {TcpReceiveEndpoint.SOH};
				options.STXDelimiters = new byte[] {TcpReceiveEndpoint.STX};
				options.ETXDelimiters = new byte[] {TcpReceiveEndpoint.ETX};
				options.EOTDelimiters = new byte[] {TcpReceiveEndpoint.EOT};

				TcpClient client = new TcpClient();
				client.Connect(IPAddress.Loopback, 6789);
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.SOH}, 0, 1);

				// Lets try a bit more message .. just for the fun of it ..
				for (int i = 0; i < 100; i++)
				{
					client.GetStream().Write(new byte[] {TcpReceiveEndpoint.STX, 0x41, 0x41, 0x41, 0x41, TcpReceiveEndpoint.ETX}, 0, 6);
					receiveEndpoint.WaitForMessage();
				}
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.EOT}, 0, 1);
				client.Close();
			}
			{
				// Test with multiple bytes in delimiters

				var options = receiveEndpoint.GetOptions();
				options.SOHDelimiters = new byte[] {TcpReceiveEndpoint.SOH, 0x11};
				options.STXDelimiters = new byte[] {TcpReceiveEndpoint.STX, 0x11};
				options.ETXDelimiters = new byte[] {TcpReceiveEndpoint.ETX, 0x11};
				options.EOTDelimiters = new byte[] {TcpReceiveEndpoint.EOT, 0x11};

				TcpClient client = new TcpClient();
				client.Connect(IPAddress.Loopback, 6789);
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.SOH, 0x11}, 0, 2);
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.STX, 0x011, 0x41, 0x41, 0x41, 0x41, TcpReceiveEndpoint.ETX, 0x11}, 0, 8);
				receiveEndpoint.WaitForMessage();
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.STX, 0x011, 0x41, 0x41, 0x41, 0x41, TcpReceiveEndpoint.ETX, 0x11}, 0, 8);
				receiveEndpoint.WaitForMessage();
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.EOT}, 0, 1);
				client.Close();
			}

			{
				// Test with no SOH or EOT (typical HL7 MLLPv1)

				var options = receiveEndpoint.GetOptions();
				options.SOHDelimiters = new byte[] {};
				options.STXDelimiters = new byte[] {TcpReceiveEndpoint.STX};
				options.ETXDelimiters = new byte[] {TcpReceiveEndpoint.ETX};
				options.EOTDelimiters = new byte[] {};

				TcpClient client = new TcpClient();
				client.Connect(IPAddress.Loopback, 6789);
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.STX, 0x41, 0x41, 0x41, 0x41, TcpReceiveEndpoint.ETX}, 0, 6);
				receiveEndpoint.WaitForMessage();
				client.GetStream().Write(new byte[] {TcpReceiveEndpoint.STX, 0x41, 0x41, 0x41, 0x41, TcpReceiveEndpoint.ETX}, 0, 6);
				receiveEndpoint.WaitForMessage();
				client.Close();
			}

			// Check that endpoint received the message
			MockEndpoint mockEndpoint = sendEndpoint as MockEndpoint;
			MockEndpoint mockEndpoint2 = sendEndpoint2 as MockEndpoint;
			Assert.IsNotNull(mockEndpoint);
			Assert.IsNotNull(mockEndpoint.Messages);
			Assert.AreEqual(104, mockEndpoint.Messages.Count);
			Assert.AreEqual(104, mockEndpoint2.Messages.Count);
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
