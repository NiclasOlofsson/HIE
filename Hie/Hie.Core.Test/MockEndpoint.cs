using System.Collections.Generic;
using Hie.Core.Endpoints;
using Hie.Core.Model;

namespace Hie.Core.Test
{
	public class MockEndpoint : EndpointBase
	{
		internal List<Message> Messages { get; set; }

		public MockEndpoint(EndpointDirection direction) : base()
		{
			Messages = new List<Message>();
		}

		public override void StopProcessing()
		{
			throw new System.NotImplementedException();
		}

		public override void ProcessMessage(object source, Message message)
		{
			Messages.Add(message);
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
