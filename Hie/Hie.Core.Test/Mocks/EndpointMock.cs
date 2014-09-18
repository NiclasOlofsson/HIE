using System.Collections.Generic;
using Hie.Core.Endpoints;
using Hie.Core.Model;

namespace Hie.Core.Mocks
{
	public class EndpointMock : EndpointBase
	{
		internal List<byte[]> Messages { get; set; }

		public EndpointMock()
		{
			Messages = new List<byte[]>();
		}

		public override void StopProcessing()
		{
			throw new System.NotImplementedException();
		}

		public override void ProcessMessage(object source, Message message)
		{
			throw new System.NotImplementedException();
		}

		public override void ProcessMessage(IEndpoint endpoint, byte[] data)
		{
			Messages.Add(data);
		}

		public override void Initialize(IOptions options)
		{
			throw new System.NotImplementedException();
		}

		public override void StartProcessing()
		{
		}

		public void SendTestMessage(Message testMessage)
		{
			HostService.PublishMessage(this, testMessage);
		}
	}
}
