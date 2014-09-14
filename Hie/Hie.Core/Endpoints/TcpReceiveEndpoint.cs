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
		public const byte SOH = 0x01;
		public const byte STX = 0x02;
		public const byte ETX = 0x03;
		public const byte EOT = 0x04;


		private readonly IPEndPoint _endpoint;

		public class Options : IOptions
		{
			public IPEndPoint Endpoint { get; set; }
			public byte[] MessageStartDelimiters { get; set; }
			public byte[] MessageEndDelimiters { get; set; }
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
			StateObject state = new StateObject();
			state.workSocket = client;
			client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);

			listener.BeginAcceptSocket(AcceptCallback, listener);
		}


		public void ReadCallback(IAsyncResult ar)
		{
			StateObject state = (StateObject) ar.AsyncState;
			Socket handler = state.workSocket;

			// Read data from the client socket. 
			int bytesRead = handler.EndReceive(ar);

			if (bytesRead > 0)
			{
				bool endOfMessage = false;

				foreach (var b in state.buffer)
				{
					if (b != SOH && b != STX && b != ETX && b != EOT)
					{
						state.sb.Append(Encoding.ASCII.GetString(new byte[] {b}, 0, 1));
					}
					if (b == ETX || b == EOT)
					{
						endOfMessage = true;
						break;
					}
				}

				// Check for end-of-file tag. If it is not there, read 
				// more data.
				if (endOfMessage)
				{
					Message message = new Message("text/plain");
					message.Value = state.sb.ToString();
					HostService.PublishMessage(this, message);
					MessageSent.Set();

					state = new StateObject();
					state.workSocket = handler;
					handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
				}
				else
				{
					// Not all data received. Get more.
					handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
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
