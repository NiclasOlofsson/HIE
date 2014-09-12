using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Hie.Core.Model
{
	public enum EndpointTargetType
	{
		Unkown,
		Broadcast,
		Direct
	}

	public enum EndpointDirection
	{
		Unknown,
		OneWayReceive,
		OneWaySend,
		RequestResponseSend,
		RequestResponseReceive
	}

	public class Endpoint
	{
		public EndpointDirection Direction { get; set; }
		public Channel DirectTarget { get; set; }
		public ApplicationHost HostService { get; set; }
		public Dictionary<string, object> EndpointMap { get; set; }

		public Endpoint()
		{
			Direction = EndpointDirection.Unknown;
			EndpointMap = new Dictionary<string, object>();
		}

		protected void SendMessage(Message message)
		{
			switch (Direction)
			{
				case EndpointDirection.RequestResponseReceive:
				case EndpointDirection.OneWayReceive:
					HostService.RouteMessage(this, DirectTarget, message);
					break;
				case EndpointDirection.Unknown:
				case EndpointDirection.OneWaySend:
				case EndpointDirection.RequestResponseSend:
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public virtual void ProcessMessage(object source, Message message)
		{
			switch (Direction)
			{
				case EndpointDirection.OneWaySend:
					break;
				case EndpointDirection.RequestResponseSend:
					break;
				case EndpointDirection.RequestResponseReceive:
				case EndpointDirection.OneWayReceive:
				case EndpointDirection.Unknown:
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public virtual void StartProcessing()
		{
		}
	}

	public class TcpReceiveEndpoint : Endpoint
	{
		public readonly ManualResetEvent MessageSent = new ManualResetEvent(false);

		private int _port;
		private TcpListener _listener;

		public TcpReceiveEndpoint(int port)
		{
			_port = port;
			Direction = EndpointDirection.OneWayReceive;
		}

		public override void StartProcessing()
		{
			_listener = new TcpListener(IPAddress.Any, _port);

			_listener.Start();
			_listener.BeginAcceptSocket(Callback, _listener);
		}

		private void Callback(IAsyncResult ar)
		{
			TcpListener listener = (TcpListener) ar.AsyncState;
			Socket client = listener.EndAcceptSocket(ar);
			SendMessage(new Message());
			MessageSent.Set();

			listener.BeginAcceptSocket(Callback, listener);
		}

		public void WaitForMessage(int milisecondsTimeout = 1000)
		{
			MessageSent.WaitOne(milisecondsTimeout);
			MessageSent.Reset();
		}
	}

	public class FileReaderEndpoint : Endpoint
	{
		private string _filePath;
		private Timer _timer;
		private int _pollingInterval;
		private Encoding _encoding;

		public readonly ManualResetEvent MessageSent = new ManualResetEvent(false);

		public FileReaderEndpoint(string filePath, int pollingInterval, Encoding encoding)
		{
			Direction = EndpointDirection.OneWayReceive;
			_filePath = filePath;
			_pollingInterval = pollingInterval;
			_encoding = encoding;
		}

		public override void StartProcessing()
		{
			_timer = new Timer();
			_timer.Interval = _pollingInterval;
			_timer.AutoReset = true;
			_timer.Elapsed += TimerOnElapsed;
			_timer.Start();

			if (!File.Exists(_filePath))
			{
				throw new FileNotFoundException(string.Format("Unable to locate file '{0}'", _filePath), _filePath);
			}
		}

		private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
		{
			using (StreamReader reader = new StreamReader(_filePath, _encoding))
			{
				Message message = new Message();
				message.Value = reader.ReadToEnd();
				reader.Close();
				SendMessage(message);
				MessageSent.Set();
			}
		}

		public void WaitForMessage(int milisecondsTimeout = 1000)
		{
			MessageSent.WaitOne(milisecondsTimeout);
			MessageSent.Reset();
		}
	}

	public class FileWriterEndpoint : Endpoint
	{
		private string _filePath;
		private Encoding _encoding;
		private readonly bool _deleteOnStart;
		private bool _append;

		public FileWriterEndpoint(string filePath, bool append, Encoding encoding, bool deleteOnStart = false)
		{
			Direction = EndpointDirection.OneWaySend;
			_filePath = filePath;
			_encoding = encoding;
			_deleteOnStart = deleteOnStart;
			_append = append;
		}

		public override void StartProcessing()
		{
			if (_deleteOnStart && File.Exists(_filePath))
			{
				File.Delete(_filePath);
			}
		}

		public override void ProcessMessage(object source, Message message)
		{
			using (StreamWriter writer = new StreamWriter(_filePath, _append, _encoding))
			{
				writer.Write(message.Value);
				writer.Close();
				MessageSent.Set();
			}
		}

		public readonly ManualResetEvent MessageSent = new ManualResetEvent(false);

		public void WaitForMessage(int milisecondsTimeout = 1000)
		{
			MessageSent.WaitOne(milisecondsTimeout);
			MessageSent.Reset();
		}
	}
}
