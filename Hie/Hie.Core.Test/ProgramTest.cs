using System;
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

			Message testMessage = new Message {Value = BuildJsonString()};
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
				Assert.AreEqual("Hello world!" + Environment.NewLine + "Hello world!" + Environment.NewLine, text);
				reader.Close();
			}
		}

		private string BuildJsonString()
		{
			string retJson =
				"{" +
				"  'HL7Message': {" +
				"    'MSH': {" +
				"      'MSH.1': '|'," +
				"      'MSH.2': '^~\\&'," +
				"      'MSH.3': { 'MSH.3.1': 'ULTRAGENDAPRO' }," +
				"      'MSH.4': { 'MSH.4.1': 'BMI' }," +
				"      'MSH.5': { 'MSH.5.1': 'WinPath HL7' }," +
				"      'MSH.6': { 'MSH.6.1': 'WinPath HL7' }," +
				"      'MSH.7': { 'MSH.7.1': '20120723020203' }," +
				"      'MSH.9': {" +
				"        'MSH.9.1': 'ADT'," +
				"        'MSH.9.2': 'A01'" +
				"      }," +
				"      'MSH.10': { 'MSH.10.1': '634786057279082040' }," +
				"      'MSH.11': { 'MSH.11.1': 'P' }," +
				"      'MSH.12': { 'MSH.12.1': '2.3' }," +
				"      'MSH.15': { 'MSH.15.1': 'NE' }," +
				"      'MSH.16': { 'MSH.16.1': 'AL' }," +
				"      'MSH.18': { 'MSH.18.1': 'ASCII' }" +
				"    }," +
				"    'EVN': {" +
				"      'EVN.1': { 'EVN.1.1': 'A01' }," +
				"      'EVN.2': { 'EVN.2.1': '20120723020203' }" +
				"    }," +
				"    'PID': {" +
				"      'PID.3': {" +
				"        'PID.3.1': 'CCH3194057'," +
				"        'PID.3.5': 'PASID'" +
				"      }," +
				"      'PID.4': { 'PID.4.1': '11242757' }," +
				"      'PID.5': {" +
				"        'PID.5.1': 'Surnom1'," +
				"        'PID.5.2': 'Forename1'," +
				"        'PID.5.5': 'Mrs'" +
				"      }," +
				"      'PID.7': { 'PID.7.1': '19580525000000' }," +
				"      'PID.8': { 'PID.8.1': 'F' }," +
				"      'PID.9': { 'PID.9.7': 'PG' }," +
				"      'PID.11': {" +
				"        'PID.11.1': 'Add1'," +
				"        'PID.11.2': 'add2'," +
				"        'PID.11.3': 'town'," +
				"        'PID.11.5': 'postcode'," +
				"        'PID.11.7': 'P'" +
				"      }," +
				"      'PID.13': {" +
				"        'PID.13.10': '~'," +
				"        'PID.13.12': 'FX'" +
				"      }," +
				"      'PID.30': { 'PID.30.1': '0' }" +
				"    }," +
				"    'PV1': {" +
				"      'PV1.2': { 'PV1.2.1': 'I' }," +
				"      'PV1.3': {" +
				"        'PV1.3.1': 'CCHWDDOW'," +
				"        'PV1.3.2': 'Ward Downing'," +
				"        'PV1.3.7': 'CCH'," +
				"        'PV1.3.9': 'The Clementine Churchill Hospital'" +
				"      }," +
				"      'PV1.9': {" +
				"        'PV1.9.1': 'CBMI7655'," +
				"        'PV1.9.2': 'Pathology Interface'," +
				"        'PV1.9.3': 'CCH'," +
				"        'PV1.9.9': '~CBMI7655'" +
				"      }," +
				"      'PV1.10': { 'PV1.10.1': 'INP' }," +
				"      'PV1.19': { 'PV1.19.1': '37436686' }," +
				"      'PV1.44': { 'PV1.44.1': '20120723' }" +
				"    }" +
				"  }" +
				"}";

			return retJson;
		}
	}
}
