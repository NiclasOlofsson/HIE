using System.Collections.Generic;
using Hie.Core.Model;

namespace Hie.Core.Test
{
	public class MockEndpoint : Endpoint
	{
		internal List<Message> Messages { get; set; }

		public MockEndpoint()
		{
			Messages = new List<Message>();
		}

		public override void ProcessMessage(object source, Message message)
		{
			base.ProcessMessage(source, message);

			Messages.Add(message);
		}

		public void SendTestMessage(Message testMessage)
		{
			SendMessage(testMessage);
		}
	}
}
