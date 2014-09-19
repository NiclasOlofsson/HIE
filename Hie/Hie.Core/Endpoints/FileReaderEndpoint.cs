using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using Hie.Core.Model;
using Timer = System.Timers.Timer;

namespace Hie.Core.Endpoints
{
	public class FileReaderEndpoint : EndpointBase
	{
		private string _filePath;
		private Timer _timer;
		private int _pollingInterval;
		private Encoding _encoding;

		public readonly ManualResetEvent MessageSent = new ManualResetEvent(false);
		protected IApplicationHost _hostService;

		public FileReaderEndpoint(string filePath, int pollingInterval, Encoding encoding) : base()
		{
			_filePath = filePath;
			_pollingInterval = pollingInterval;
			_encoding = encoding;
		}

		public override void Initialize(IApplicationHost host, IOptions options)
		{
			_hostService = host;
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

		public override void StopProcessing()
		{
			throw new System.NotImplementedException();
		}

		public override void ProcessMessage(IEndpoint endpoint, byte[] data)
		{
		}

		private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
		{
			using (StreamReader reader = new StreamReader(_filePath, _encoding))
			{
				string content = reader.ReadToEnd();
				reader.Close();

				_hostService.ProcessInPipeline(this, Encoding.UTF8.GetBytes(content));

				MessageSent.Set();
			}
		}

		public void WaitForMessage(int milisecondsTimeout = 1000)
		{
			MessageSent.WaitOne(milisecondsTimeout);
			MessageSent.Reset();
		}
	}
}
