using System.Collections.Generic;
using Hie.Core.Endpoints;
using Hie.Core.Model;

namespace Hie.Core.Mocks
{
	public class EndpointMock : EndpointBase
	{
		protected IApplicationHost _hostService;
		internal List<byte[]> Messages { get; set; }

		public EndpointMock()
		{
			Messages = new List<byte[]>();
		}

		public override void StopProcessing()
		{
			throw new System.NotImplementedException();
		}

		public override void ProcessMessage(IEndpoint endpoint, byte[] data)
		{
			Messages.Add(data);
		}

		public override void Initialize(IApplicationHost host, IOptions options)
		{
			_hostService = host;
		}

		public override void StartProcessing()
		{
		}

		public void SendTestMessage(Message testMessage)
		{
			_hostService.PublishMessage(this, testMessage);
		}
	}
}
