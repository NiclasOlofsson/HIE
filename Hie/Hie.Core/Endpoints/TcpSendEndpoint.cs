using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hie.Core.Model;

namespace Hie.Core.Endpoints
{
	public class TcpSendEndpoint : EndpointBase
	{
		private TcpClient _client;
		private TcpSendOptions _options;

		public override void Initialize(IOptions options)
		{
			_options = (TcpSendOptions) options;

			// Validate options (since this will be coming from a human)
			_options.Validate();
		}

		public override void StartProcessing()
		{
		}

		public override void StopProcessing()
		{
			if (_client != null && _client.Connected)
			{
				CloseConnection();
			}
		}

		public override void ProcessMessage(object source, Message message)
		{
		}

		public override void ProcessMessage(IEndpoint endpoint, byte[] data)
		{
			List<byte> package = new List<byte>();
			if (_client == null)
			{
				_client = new TcpClient();
				_client.Connect(_options.Endpoint);

				if (_options.SohDelimiters.Length > 0)
				{
					package.AddRange(_options.SohDelimiters);
				}
			}

			//TODO: If options mandate delimters, write STX and ETX here
			if (_options.StxDelimiters.Length > 0)
			{
				package.AddRange(_options.StxDelimiters);
			}

			package.AddRange(data);

			if (_options.EtxDelimiters.Length > 0)
			{
				package.AddRange(_options.EtxDelimiters);
			}

			Write(package.ToArray());

			if (!_options.KeepConnectionOpen)
			{
				CloseConnection();
			}
		}

		private object _closeLock = new object();

		private void CloseConnection()
		{
			lock (_closeLock)
			{
				if (_options.EotDelimiters.Length > 0)
				{
					Write(_options.EotDelimiters, false);
				}

				_client.Close();
				_client = null;
			}
		}

		private void Write(byte[] data, bool async = true)
		{
			var stream = _client.GetStream();
			if (async) stream.BeginWrite(data, 0, data.Length, WriteCompleteCallback, stream);
			else stream.Write(data, 0, data.Length);
		}

		private void WriteCompleteCallback(IAsyncResult ar)
		{
			lock (_closeLock)
			{
				if (_client != null && _client.Connected)
				{
					NetworkStream stream = (NetworkStream) ar.AsyncState;
					stream.EndWrite(ar);
				}
			}
		}
	}

	public class TcpSendOptions : IOptions
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

		public bool KeepConnectionOpen { get; set; }

		// Socket options
		public bool NoDelay { get; set; }
		public int ReceiveBufferSize { get; set; }
		public int SendBufferSize { get; set; }

		public TcpSendOptions()
		{
			// Defaults
			SohDelimiters = new[] { SOH };
			StxDelimiters = new[] { STX };
			EtxDelimiters = new[] { ETX };
			EotDelimiters = new[] { EOT };

			KeepConnectionOpen = false;

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
}
