using System.IO;
using System.Text;
using Hie.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core.Endpoints
{
	[TestClass]
	public class FileReceiverEndpointTest
	{
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
