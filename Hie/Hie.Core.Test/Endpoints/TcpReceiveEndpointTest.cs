using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hie.Core.Endpoints
{
	[TestClass]
	public class TcpReceiveEndpointTest
	{
		[TestMethod]
		public void EndpointInterfaceLifecycleTest()
		{
			// Host
			var host = new Mock<IApplicationHost>();

			// Endpoint to test
			TcpReceiveEndpoint receiveEndpoint = new TcpReceiveEndpoint();
			receiveEndpoint.HostService = host.Object;

			receiveEndpoint.Init(new Options() {Endpoint = new IPEndPoint(IPAddress.Any, 6789), NoDelay = true, ReceiveBufferSize = 8192});
			receiveEndpoint.StartProcessing();

			// Try connection two clients and process messages

			Stopwatch watch = new Stopwatch();
			watch.Start();
			{
				TcpClient client = new TcpClient();
				client.NoDelay = true;
				client.Connect(IPAddress.Loopback, 6789);
				client.GetStream().Write(new byte[] {Options.SOH}, 0, 1);

				// Lets try a bit more message .. just for the fun of it ..
				for (int i = 0; i < 100; i++)
				{
					client.GetStream().Write(new byte[] {Options.STX, 0x41, 0x41, 0x41, 0x41, Options.ETX}, 0, 6);
					receiveEndpoint.WaitForMessage();
				}
				client.GetStream().Write(new byte[] {Options.EOT}, 0, 1);
				client.Close();
			}

			//Assert.IsFalse(true, "{0}", watch.ElapsedMilliseconds);

			{
				TcpClient client = new TcpClient();
				client.NoDelay = true;
				client.Connect(IPAddress.Loopback, 6789);
				client.GetStream().Write(new byte[] {Options.SOH}, 0, 1);
				client.GetStream().Write(new byte[] {Options.STX, 0x41, 0x41, 0x41, 0x41, Options.ETX}, 0, 6);
				receiveEndpoint.WaitForMessage();
				client.GetStream().Write(new byte[] {Options.STX, 0x41, 0x41, 0x41, 0x41, Options.ETX}, 0, 6);
				receiveEndpoint.WaitForMessage();
				client.GetStream().Write(new byte[] {Options.EOT}, 0, 1);
				client.Close();
			}

			receiveEndpoint.StopProcessing();
			host.Verify(app => app.ProcessInPipeline(It.IsAny<TcpReceiveEndpoint>(), It.IsNotNull<byte[]>()), Times.Exactly(102));
			host.Verify(app => app.ProcessInPipeline(It.IsAny<TcpReceiveEndpoint>(), It.Is<byte[]>(indata => indata.SequenceEqual(new byte[] {0x41, 0x41, 0x41, 0x41}))));
		}


		[TestMethod]
		public void TcpReceiveEndpointSingleByteDelimitersTest()
		{
			var options = new Options();
			TcpReceiveEndpoint endpoint = new TcpReceiveEndpoint(new IPEndPoint(IPAddress.Any, 6789), options);
			var host = new Mock<IApplicationHost>();
			endpoint.HostService = host.Object;

			StateObject state = new StateObject(null);

			List<byte> data = new List<byte>();
			data.AddRange(new byte[] {Options.SOH});
			data.AddRange(new byte[] {Options.STX, 0x41, 0x41, 0x41, 0x41, Options.ETX});
			data.AddRange(new byte[] {Options.EOT});
			data.CopyTo(state.Buffer, 0);

			bool isEot = endpoint.ProcessIncomingStream(state.Buffer.Length, state);

			Assert.IsTrue(isEot);
			host.Verify(app => app.ProcessInPipeline(It.IsAny<TcpReceiveEndpoint>(), It.IsNotNull<byte[]>()), Times.Once);
			host.Verify(app => app.ProcessInPipeline(It.IsAny<TcpReceiveEndpoint>(), It.Is<byte[]>(indata => indata.SequenceEqual(new byte[] {0x41, 0x41, 0x41, 0x41}))));
		}

		[TestMethod]
		public void TcpReceiveEndpointMultiByteDelimitersTest()
		{
			var options = new Options();
			options.SohDelimiters = new byte[] {Options.SOH, 0x06};
			options.StxDelimiters = new byte[] {Options.STX, 0x06};
			options.EtxDelimiters = new byte[] {Options.ETX, 0x06};
			options.EotDelimiters = new byte[] {Options.EOT, 0x07};
			//BUG: If the end-delimiter of STX and EOT match it will not detect EOT
			//options.EOTDelimiters = new byte[] {TcpReceiveEndpoint.EOT, 0x06};

			TcpReceiveEndpoint endpoint = new TcpReceiveEndpoint(new IPEndPoint(IPAddress.Any, 6789), options);
			var host = new Mock<IApplicationHost>();
			endpoint.HostService = host.Object;

			StateObject state = new StateObject(null);

			List<byte> data = new List<byte>();
			data.AddRange(options.SohDelimiters);
			data.AddRange(options.StxDelimiters);
			data.AddRange(new byte[] {0x41, 0x41, 0x41, 0x41});
			data.AddRange(options.EtxDelimiters);
			data.AddRange(options.EotDelimiters);
			data.CopyTo(state.Buffer, 0);

			bool isEot = endpoint.ProcessIncomingStream(state.Buffer.Length, state);

			Assert.IsTrue(isEot);
			host.Verify(app => app.ProcessInPipeline(It.IsAny<TcpReceiveEndpoint>(), It.IsNotNull<byte[]>()), Times.Once);
			host.Verify(app => app.ProcessInPipeline(It.IsAny<TcpReceiveEndpoint>(), It.Is<byte[]>(indata => indata.SequenceEqual(new byte[] {0x41, 0x41, 0x41, 0x41}))));
		}

		[TestMethod]
		public void TcpReceiveEndpointMultiByteDelimitersSplittedBufferTest()
		{
			var options = new Options();
			options.SohDelimiters = new byte[] {Options.SOH, 0x06};
			options.StxDelimiters = new byte[] {Options.STX, 0x06};
			options.EtxDelimiters = new byte[] {Options.ETX, 0x06};
			options.EotDelimiters = new byte[] {Options.EOT, 0x07};

			TcpReceiveEndpoint endpoint = new TcpReceiveEndpoint(new IPEndPoint(IPAddress.Any, 6789), options);
			var host = new Mock<IApplicationHost>();
			endpoint.HostService = host.Object;

			StateObject state = new StateObject(null);

			List<byte> data = new List<byte>();
			data.AddRange(options.SohDelimiters);
			data.AddRange(options.StxDelimiters);
			data.AddRange(new byte[] {0x41, 0x41, 0x41, 0x41});
			data.AddRange(options.EtxDelimiters);
			data.AddRange(options.EotDelimiters);

			bool isEot = false;

			for (int i = 0; i < data.Count; i++)
			{
				state.State = StateObject.FrameState.FindSoh;

				int noBytesInFirstBuffer = i;
				data.CopyTo(0, state.Buffer, 0, noBytesInFirstBuffer);

				isEot = endpoint.ProcessIncomingStream(noBytesInFirstBuffer, state);
				Assert.IsFalse(isEot);

				data.CopyTo(noBytesInFirstBuffer, state.Buffer, 0, data.Count - noBytesInFirstBuffer);
				isEot = endpoint.ProcessIncomingStream(data.Count - noBytesInFirstBuffer, state);
				Assert.IsTrue(isEot);
			}

			host.Verify(app => app.ProcessInPipeline(It.IsAny<TcpReceiveEndpoint>(), It.IsNotNull<byte[]>()), Times.AtLeast(3));
			host.Verify(app => app.ProcessInPipeline(It.IsAny<TcpReceiveEndpoint>(), It.Is<byte[]>(indata => indata.SequenceEqual(new byte[] {0x41, 0x41, 0x41, 0x41}))));
		}


		[TestMethod]
		public void TcpReceiveEndpointMissingTransmissionDelimitersTest()
		{
			var options = new Options();
			options.SohDelimiters = new byte[] {};
			options.EotDelimiters = new byte[] {};

			TcpReceiveEndpoint endpoint = new TcpReceiveEndpoint(new IPEndPoint(IPAddress.Any, 6789), options);
			var host = new Mock<IApplicationHost>();
			endpoint.HostService = host.Object;

			StateObject state = new StateObject(null);

			List<byte> data = new List<byte>();
			data.AddRange(options.SohDelimiters);
			data.AddRange(options.StxDelimiters);
			data.AddRange(new byte[] {0x41, 0x41, 0x41, 0x41});
			data.AddRange(options.EtxDelimiters);
			data.AddRange(options.EotDelimiters);
			data.CopyTo(state.Buffer, 0);

			bool isEot = endpoint.ProcessIncomingStream(state.Buffer.Length, state);

			Assert.IsFalse(isEot);

			host.Verify(app => app.ProcessInPipeline(It.IsAny<TcpReceiveEndpoint>(), It.IsNotNull<byte[]>()), Times.Once);
			host.Verify(app => app.ProcessInPipeline(It.IsAny<TcpReceiveEndpoint>(), It.Is<byte[]>(indata => indata.SequenceEqual(new byte[] {0x41, 0x41, 0x41, 0x41}))));
		}
	}
}
