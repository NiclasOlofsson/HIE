using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Hie.Core.Model;

namespace Hie.Core.Endpoints
{
	public class TcpReceiveEndpoint : EndpointBase
	{
		private readonly IPEndPoint _endpoint;

		public class Options : IOptions
		{
			public IPEndPoint Endpoint { get; set; }
		}

		public class StateObject
		{
			// Client  socket.
			public Socket workSocket = null;
			// Size of receive buffer.
			public const int BufferSize = 1024;
			// Receive buffer.
			public byte[] buffer = new byte[BufferSize];
			// Received data string.
			public StringBuilder sb = new StringBuilder();
		}

		public readonly ManualResetEvent MessageSent = new ManualResetEvent(false);

		private TcpListener _listener;

		public TcpReceiveEndpoint(IPEndPoint endpoint)
		{
			_endpoint = endpoint;
		}

		public override void StartProcessing()
		{
			_listener = new TcpListener(_endpoint);
			//_listener.Server.NoDelay Investigate!
			_listener.Start();
			_listener.BeginAcceptSocket(AcceptCallback, _listener);
		}

		public override void StopProcessing()
		{
			throw new NotImplementedException();
		}

		public override void ProcessMessage(object source, Message message)
		{
		}

		private void AcceptCallback(IAsyncResult ar)
		{
			TcpListener listener = (TcpListener) ar.AsyncState;
			Socket client = listener.EndAcceptSocket(ar);

			// Mock stuff - remove later
			HostService.PublishMessage(this, DirectTarget, new Message());
			MessageSent.Set();

			listener.BeginAcceptSocket(AcceptCallback, listener);
		}


		public static void ReadCallback(IAsyncResult ar)
		{
			StateObject state = (StateObject) ar.AsyncState;
			Socket handler = state.workSocket;

			// Read data from the client socket. 
			int bytesRead = handler.EndReceive(ar);

			if (bytesRead > 0)
			{
				// There  might be more data, so store the data received so far.
				state.sb.Append(Encoding.ASCII.GetString(
					state.buffer, 0, bytesRead));

				// Check for end-of-file tag. If it is not there, read 
				// more data.
				if (false)
				{
				}
				else
				{
					// Not all data received. Get more.
					handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
						new AsyncCallback(ReadCallback), state);
				}
			}
		}

		public void WaitForMessage(int milisecondsTimeout = 1000)
		{
			MessageSent.WaitOne(milisecondsTimeout);
			MessageSent.Reset();
		}
	}
}
