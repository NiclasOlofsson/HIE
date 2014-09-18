using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Hie.Core.Model;

namespace Hie.Core.Endpoints
{
	public class TcpReceiveEndpoint : EndpointBase
	{
		public readonly ManualResetEvent MessageSent = new ManualResetEvent(false);

		private TcpListener _listener;
		private TcpReceieveOptions _options;

		public TcpReceiveEndpoint()
		{
		}

		// For test
		internal TcpReceiveEndpoint(IPEndPoint endpoint = null, TcpReceieveOptions options = null)
		{
			_options = options ?? new TcpReceieveOptions();
			_options.Endpoint = endpoint;
		}

		public override void Initialize(IOptions options)
		{
			_options = (TcpReceieveOptions) options;

			// Validate options (since this will be coming from a human)
			_options.Validate();
		}

		public override void StartProcessing()
		{
			_options.Validate();

			_listener = new TcpListener(_options.Endpoint);
			_listener.Start();
			_listener.BeginAcceptSocket(AcceptCallback, _listener);
		}

		public override void StopProcessing()
		{
			_listener.Stop();
		}

		public override void ProcessMessage(object source, Message message)
		{
			//TODO: Implement send-side of things (maybe)
		}

		public override void ProcessMessage(IEndpoint endpoint, byte[] data)
		{
			throw new NotImplementedException();
		}

		private void AcceptCallback(IAsyncResult ar)
		{
			TcpListener listener = (TcpListener) ar.AsyncState;

			// The listener might have been stopped. 
			// Avoid exceptions and stop listenting for more accepts.
			if (listener.Server == null || !listener.Server.IsBound)
				return;

			Socket socket = listener.EndAcceptSocket(ar);
			socket.NoDelay = _options.NoDelay;
			socket.ReceiveBufferSize = _options.ReceiveBufferSize;
			socket.SendBufferSize = _options.SendBufferSize;
			// For future, this have no effect on async communication scenarios:
			// - socket.ReceiveTimeout
			// - socket.SendTimeout
			// Implement using semaphores instead.

			StateObject state = new StateObject(socket, _options.ReceiveBufferSize);
			socket.BeginReceive(state.Buffer, 0, state.BufferSize, 0, ReadCallback, state);

			listener.BeginAcceptSocket(AcceptCallback, listener);
		}


		private void ReadCallback(IAsyncResult ar)
		{
			StateObject state = (StateObject) ar.AsyncState;
			Socket socket = state.WorkSocket;

			// Read data from the client socket. 
			int bytesRead = socket.EndReceive(ar);

			if (bytesRead > 0)
			{
				bool endOfTransmission = ProcessIncomingStream(bytesRead, state);

				// Check for end-of-file tag. If it is not there, read 
				// more data.
				if (!endOfTransmission)
				{
					// Not all data received. Get more.
					socket.BeginReceive(state.Buffer, 0, state.BufferSize, 0, ReadCallback, state);
				}
			}

			// Will simply shutdown communication if nothing received
		}

		internal bool ProcessIncomingStream(int bytesRead, StateObject state)
		{
			bool endOfTransmission = false;

			for (int i = 0; (i < bytesRead && !endOfTransmission); i++)
			{
				state.Stream.WriteByte(state.Buffer[i]);

				switch (state.State)
				{
					case StateObject.FrameState.FindSoh:
					{
						if (_options.SohDelimiters.Length > 0)
						{
							if (state.Stream.Position < _options.SohDelimiters.Length)
							{
								continue;
							}

							bool foundDelimiters = CheckDelimiter(_options.SohDelimiters, state.Stream);

							if (foundDelimiters)
							{
								state.Stream.Position = 0;
								state.State = StateObject.FrameState.FindStx;
							}

							continue;
						}
						else
						{
							state.State = StateObject.FrameState.FindStx;
							goto case StateObject.FrameState.FindStx;
						}
					}
					case StateObject.FrameState.FindStx:
					{
						if (_options.EotDelimiters.Length == 0)
						{
							if (state.Stream.Position < _options.StxDelimiters.Length)
							{
								continue;
							}
						}
						else
						{
							if (state.Stream.Position < _options.StxDelimiters.Length && state.Stream.Position < _options.EotDelimiters.Length)
							{
								continue;
							}
						}

						bool foundEotDelimiters = CheckDelimiter(_options.EotDelimiters, state.Stream);
						bool foundStxDelimiters = CheckDelimiter(_options.StxDelimiters, state.Stream);

						if (foundEotDelimiters && foundStxDelimiters)
						{
							// Do nothing for now, but this is a really stupid case that "could" potentially happen
						}

						if (foundEotDelimiters && !foundStxDelimiters)
						{
							// Done, exit gracefuylly
							endOfTransmission = true;
							break; // consider return here
						}

						if (foundStxDelimiters && !foundEotDelimiters)
						{
							// Start search for ETX
							state.Stream.Position = 0;
							state.State = StateObject.FrameState.FindEtx;
						}

						continue;
					}
					case StateObject.FrameState.FindEtx:
					{
						if (state.Stream.Position < _options.EtxDelimiters.Length)
						{
							continue;
						}

						bool foundEtxDelimiters = CheckDelimiter(_options.EtxDelimiters, state.Stream);
						if (foundEtxDelimiters)
						{
							long pos = state.Stream.Position;
							byte[] data = new byte[(pos - _options.EtxDelimiters.Length)];
							state.Stream.Position = 0;
							state.Stream.Read(data, 0, (int) (pos - _options.EtxDelimiters.Length));

							SubmitPayloadToPipeline(data);

							// Done, change state
							state.Stream.Position = 0;
							state.State = StateObject.FrameState.FindStx;
						}

						continue;
					}
				}
			}

			return endOfTransmission;
		}

		private bool CheckDelimiter(byte[] delimiters, MemoryStream stream)
		{
			if (delimiters.Length == 0) return false;

			long pos = stream.Position;
			stream.Seek(-1*delimiters.Length, SeekOrigin.Current);
			byte[] bytes = new byte[delimiters.Length];
			stream.Read(bytes, 0, bytes.Length);

			stream.Position = pos;
			return ByteArrayCompare(bytes, delimiters);
		}

		private bool ByteArrayCompare(byte[] a1, byte[] a2)
		{
			if (a1.Length != a2.Length) return false;

			for (int i = 0; i < a1.Length; i++) if (a1[i] != a2[i]) return false;

			return true;
		}

		private void SubmitPayloadToPipeline(byte[] data)
		{
			HostService.ProcessInPipeline(this, data);
			MessageSent.Set();
		}

		public void WaitForMessage(int milisecondsTimeout = 1000)
		{
			MessageSent.WaitOne(milisecondsTimeout);
			MessageSent.Reset();
		}
	}

	public class TcpReceieveOptions : IOptions
	{
		public const byte SOH = 0x01;
		public const byte STX = 0x02;
		public const byte ETX = 0x03;
		public const byte EOT = 0x04;

		public IPEndPoint Endpoint { get; set; }

		public byte[] EotDelimiters { get; set; }
		public byte[] SohDelimiters { get; set; }
		public byte[] StxDelimiters { get; set; }
		public byte[] EtxDelimiters { get; set; }

		// Socket options
		public bool NoDelay { get; set; }
		public int ReceiveBufferSize { get; set; }
		public int SendBufferSize { get; set; }

		public TcpReceieveOptions()
		{
			// Defaults
			SohDelimiters = new[] {SOH};
			StxDelimiters = new[] {STX};
			EtxDelimiters = new[] {ETX};
			EotDelimiters = new[] {EOT};

			// Selected Socket settings
			NoDelay = false;
			ReceiveBufferSize = 8192;
			SendBufferSize = 8192;
		}

		public void Validate()
		{
			if (Endpoint == null) throw new ArgumentNullException("Invalid endpoint address familiy", new ArgumentNullException());
			if (Endpoint != null && Endpoint.AddressFamily != AddressFamily.InterNetwork) throw new ArgumentException("Invalid endpoint address familiy: " + Endpoint.AddressFamily + ". Only InterNetwork allowed.");

			if (StxDelimiters == null || EtxDelimiters == null) throw new ArgumentNullException("STX and ETX delimiters must be provided", new ArgumentNullException());

			if (SohDelimiters == null) SohDelimiters = new byte[0];
			if (EotDelimiters == null) EotDelimiters = new byte[0];

			if (SohDelimiters.Length == 0 && EotDelimiters.Length != 0) throw new ArgumentException("If SOH is empty then EOT also needs to be empty");
			if (SohDelimiters.Length != 0 && EotDelimiters.Length == 0) throw new ArgumentException("If EOT is empty then SOH also needs to be empty");
			if (StxDelimiters.Length == 0 || EtxDelimiters.Length == 0) throw new ArgumentException("STX and ETX delimiters must be provided");
		}
	}

	internal class StateObject
	{
		internal enum FrameState
		{
			FindSoh,
			FindStx,
			FindEtx,
		}

		internal Socket WorkSocket { get; private set; }
		internal int BufferSize { get; private set; }
		internal byte[] Buffer { get; private set; }
		internal MemoryStream Stream { get; private set; }
		internal FrameState State { get; set; }

		public StateObject(Socket workSocket, int bufferSize = 8192)
		{
			WorkSocket = workSocket;
			BufferSize = bufferSize;
			Buffer = new byte[BufferSize];
			Stream = new MemoryStream();
			State = FrameState.FindSoh;
		}
	}
}
