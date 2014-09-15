using System;
using System.Collections.Generic;
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
		public const byte SOH = 0x01;
		public const byte STX = 0x02;
		public const byte ETX = 0x03;
		public const byte EOT = 0x04;


		public class Options : IOptions
		{
			public IPEndPoint Endpoint { get; set; }

			public byte[] EOTDelimiters { get; set; }
			public byte[] SOHDelimiters { get; set; }
			public byte[] STXDelimiters { get; set; }
			public byte[] ETXDelimiters { get; set; }

			public Options()
			{
				// Defaults
				SOHDelimiters = new[] {SOH};
				STXDelimiters = new[] {STX};
				ETXDelimiters = new[] {ETX};
				EOTDelimiters = new[] {EOT};
			}
		}

		public class StateObject
		{
			internal enum FrameState
			{
				Unknown,
				FindSOH,
				FindSTX,
				FindETX,
				FindEOM
			}

			internal Socket WorkSocket { get; private set; }
			internal int BufferSize { get; private set; }
			internal byte[] Buffer { get; private set; }
			internal MemoryStream Stream { get; private set; }
			internal List<byte> DelimiterBuffer { get; private set; }
			internal FrameState State { get; set; }

			public StateObject(Socket workSocket, int bufferSize = 1024)
			{
				WorkSocket = workSocket;
				BufferSize = bufferSize;
				Buffer = new byte[BufferSize];
				Stream = new MemoryStream();
				State = FrameState.FindSOH;
				DelimiterBuffer = new List<byte>();
			}

			public void ResetState()
			{
				Buffer = new byte[BufferSize];
			}
		}

		public readonly ManualResetEvent MessageSent = new ManualResetEvent(false);

		private TcpListener _listener;
		private readonly IPEndPoint _endpoint;
		private readonly Options _options;

		public TcpReceiveEndpoint(IPEndPoint endpoint, Options options = null)
		{
			_endpoint = endpoint;
			_options = options ?? new Options();
		}

		public Options GetOptions()
		{
			return _options;
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
			Socket socket = listener.EndAcceptSocket(ar);
			StateObject state = new StateObject(socket);
			socket.BeginReceive(state.Buffer, 0, state.BufferSize, 0, ReadCallback, state);

			listener.BeginAcceptSocket(AcceptCallback, listener);
		}


		public void ReadCallback(IAsyncResult ar)
		{
			StateObject state = (StateObject) ar.AsyncState;
			Socket socket = state.WorkSocket;

			// Read data from the client socket. 
			int bytesRead = socket.EndReceive(ar);


			if (bytesRead > 0)
			{
				bool endOfTransmission = false;

				for (int i = 0; i < bytesRead || state.DelimiterBuffer.Count > 0; i++)
				{
					byte b;
					if (i >= bytesRead)
					{
						b = state.DelimiterBuffer[0];
						state.DelimiterBuffer.RemoveAt(0);
					}
					else
					{
						b = state.Buffer[i];
					}


					switch (state.State)
					{
						case StateObject.FrameState.FindSOH:
						{
							if (_options.SOHDelimiters.Length > 0)
							{
								int position = Array.IndexOf(_options.SOHDelimiters, b);
								if (position != -1 && position == state.Stream.Position)
								{
									state.Stream.WriteByte(b);
									if (state.Stream.Position == _options.SOHDelimiters.Length)
									{
										// Done, change state
										state.Stream.Position = 0;
										state.State = StateObject.FrameState.FindSTX;
									}
								}
								else
								{
									// We are not in a recognized byte-set. Waste all the bytes found so far
									state.Stream.Position = 0;
								}

								continue;
							}

							state.State = StateObject.FrameState.FindSTX;
							goto case StateObject.FrameState.FindSTX;
						}
						case StateObject.FrameState.FindSTX:
						{
							int position = Array.IndexOf(_options.STXDelimiters, b);
							if (position != -1 && position == state.Stream.Position)
							{
								state.Stream.WriteByte(b);
								if (state.Stream.Position == _options.STXDelimiters.Length)
								{
									// Done, change state
									state.Stream.Position = 0;
									state.State = StateObject.FrameState.FindETX;
								}
							}
							else
							{
								// WARN: This will not work if STH and EOT start with same characters, but SOH is shorter than EOT
								// Example: STH:0x01,0x02 EOT:0x01,0x02,0x03
								// In that case, a possible EOT will be interpreted as STX.
								position = Array.IndexOf(_options.EOTDelimiters, b);
								if (position != -1 && position == state.Stream.Position)
								{
									state.Stream.WriteByte(b);
									if (state.Stream.Position == _options.EOTDelimiters.Length)
									{
										// Done, change state
										state.Stream.Position = 0;
										endOfTransmission = true;
										break;
									}
								}

								// We are not in a recognized byte-set. Waste all the bytes found so far
								state.Stream.Position = 0;
							}

							continue;
						}
						case StateObject.FrameState.FindETX:
						{
							int position = Array.IndexOf(_options.ETXDelimiters, b);
							if (position != -1 && position == state.DelimiterBuffer.Count)
							{
								state.DelimiterBuffer.Add(b);
								if (state.DelimiterBuffer.Count == _options.ETXDelimiters.Length)
								{
									if (state.Stream.Position == 0) continue;

									SubmitPayloadToPipeline(state);

									state.DelimiterBuffer.Clear();
									state.Stream.Position = 0; // Reset stream

									// Done, change state
									state.Stream.Position = 0;
									state.State = StateObject.FrameState.FindSTX;
								}
							}
							else
							{
								// We are not in a recognized byte-set. Consider this part of the message
								state.Stream.Write(state.DelimiterBuffer.ToArray(), 0, state.DelimiterBuffer.Count);
								state.DelimiterBuffer.Clear();
								state.Stream.WriteByte(b);
							}

							continue;
						}
					}
				}

				// Check for end-of-file tag. If it is not there, read 
				// more data.
				if (!endOfTransmission)
				{
					// Not all data received. Get more.
					state.ResetState();
					socket.BeginReceive(state.Buffer, 0, state.BufferSize, 0, ReadCallback, state);
				}
			}
		}

		private void SubmitPayloadToPipeline(StateObject state)
		{
			Message message = new Message("text/plain");
			message.Value = Encoding.ASCII.GetString(state.Stream.ToArray());
			HostService.PublishMessage(this, message);
			MessageSent.Set();
		}

		public void WaitForMessage(int milisecondsTimeout = 1000)
		{
			MessageSent.WaitOne(milisecondsTimeout);
			MessageSent.Reset();
		}
	}
}
