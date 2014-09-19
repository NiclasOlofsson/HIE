using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core.Endpoints
{
	[TestClass]
	public class TcpSendEndpointTest
	{
		[TestMethod]
		public void ProcessMessageTest()
		{
			// setup the client configurations
			TcpSendEndpoint sendEndpoint = new TcpSendEndpoint();
			TcpSendOptions options = new TcpSendOptions();
			options.Endpoint = new IPEndPoint(IPAddress.Loopback, 6789);
			sendEndpoint.Initialize(null, options);

			// start a tcp listener
			TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, 6789));
			listener.Start();

			// start the enpoint for processing
			sendEndpoint.StartProcessing();

			// send messages
			byte[] data = { 0x41, 0x41, 0x41, 0x41 };
			sendEndpoint.ProcessMessage(null, data); // AAAA

			// assert listener for receive of messages
			var socket = listener.AcceptSocket();
			byte[] readData = new byte[8];
			Assert.AreEqual(8, socket.Receive(readData, 0, 8, SocketFlags.None));

			// close the client
			sendEndpoint.StopProcessing();

			// close the server
			listener.Stop();

			// assert data is valid

			byte[] expectedData = { TcpSendOptions.SOH, TcpSendOptions.STX, 0x41, 0x41, 0x41, 0x41, TcpSendOptions.ETX, TcpSendOptions.EOT };
			Assert.IsTrue(expectedData.SequenceEqual(readData));
		}
	}
}
