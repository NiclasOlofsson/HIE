using System.IO;
using System.Text;
using System.Threading;
using Hie.Core.Model;

namespace Hie.Core.Endpoints
{
	public class FileWriterEndpoint : EndpointBase
	{
		public class Options
		{
			public DirectoryInfo Directory { get; set; }
			public FileInfo File { get; set; }
			public Encoding Encoding { get; set; }
			public bool Append { get; set; }
		}

		private string _filePath;
		private Encoding _encoding;
		private readonly bool _deleteOnStart;
		private bool _append;

		public FileWriterEndpoint(string filePath, bool append, Encoding encoding, bool deleteOnStart = false) : base()
		{
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

		public override void StopProcessing()
		{
			throw new System.NotImplementedException();
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
