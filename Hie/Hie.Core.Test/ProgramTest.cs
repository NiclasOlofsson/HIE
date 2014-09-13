using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Hie.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core.Test
{
	[TestClass]
	public class ProgramTest
	{
		[TestMethod]
		public void BasicRoutingFilteringTransformationTest()
		{
			// A new application
			Application application = new Application();

			// Add endpoints
			Endpoint endpoint = new MockEndpoint();
			endpoint.Direction = EndpointDirection.OneWayReceive;
			application.Endpoints.Add(endpoint);

			Endpoint sendEndpoint = new MockEndpoint();
			sendEndpoint.Direction = EndpointDirection.OneWaySend;
			application.Endpoints.Add(sendEndpoint);

			// Add a channel
			Channel channel = new Channel();
			application.Channels.Add(channel);
			endpoint.DirectTarget = channel; // Not broadcast, so it forwards directly to channel (source)

			// Source setup
			Source source = new MockSource();
			channel.Source = source;
			source.Filters.Add(new DelegateFilter(message => true));
			source.Filters.Add(new JavaScriptFilter {Script = "true"});
			source.Transformers.Add(new Transformer());
			source.Transformers.Add(new DelegateTransformer(message => { }));
			source.Transformers.Add(new DelegateTransformer(message => { message.Value = message.Value; }));

			{
				Destination destination = new MockDestination();
				destination.Target = sendEndpoint;
				destination.Filters.Add(new DelegateFilter(message => true));
				destination.Filters.Add(new JavaScriptFilter {Script = "true"});
				destination.Transformers.Add(new DelegateTransformer(message => { }));
				destination.Transformers.Add(new DelegateTransformer(message => { message.Value = message.Value; }));
				channel.Destinations.Add(destination);
			}
			{
				// This destination will filter out the message
				Destination destination = new MockDestination();
				destination.Target = sendEndpoint;
				destination.Filters.Add(new DelegateFilter(message => false));
				channel.Destinations.Add(destination);
			}

			{
				// This destination will transform the message
				Destination destination = new MockDestination();
				destination.Target = sendEndpoint;
				destination.Filters.Add(new DelegateFilter(message => true));
				destination.Transformers.Add(new DelegateTransformer(message => { message.Value = message.Value + "test"; }));
				channel.Destinations.Add(destination);
			}

			// Host
			ApplicationHost applicationHost = new ApplicationHost();
			Assert.IsNotNull(applicationHost.Applications);
			applicationHost.Deploy(application);

			// Start the processing

			Message testMessage = new Message {Value = TestUtils.BuildHl7JsonString()};
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
			TcpReceiveEndpoint receiveEndpoint = new TcpReceiveEndpoint(6789);
			application.Endpoints.Add(receiveEndpoint);

			Endpoint sendEndpoint = new MockEndpoint();
			sendEndpoint.Direction = EndpointDirection.OneWaySend;
			application.Endpoints.Add(sendEndpoint);

			// Add a channel
			Channel channel = new Channel();
			application.Channels.Add(channel);
			receiveEndpoint.DirectTarget = channel;

			// Source setup
			Source source = new MockSource();
			channel.Source = source;

			Destination destination = new MockDestination();
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
				client.Close();
				receiveEndpoint.WaitForMessage();
			}
			{
				TcpClient client = new TcpClient();
				client.Connect(IPAddress.Loopback, 6789);
				client.Close();
				receiveEndpoint.WaitForMessage();
			}

			// Check that endpoint received the message
			MockEndpoint mockEndpoint = sendEndpoint as MockEndpoint;
			Assert.IsNotNull(mockEndpoint);
			Assert.IsNotNull(mockEndpoint.Messages);
			Assert.AreEqual(2, mockEndpoint.Messages.Count);
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
			Source source = new MockSource();
			channel.Source = source;

			Destination destination = new MockDestination();
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
