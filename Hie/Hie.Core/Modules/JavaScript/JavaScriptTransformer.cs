using Hie.Core.Model;
using Jint;

namespace Hie.Core.Modules.JavaScript
{
	public class JavaScriptTransformer : ITransformer
	{
		public string Script { get; set; }

		public void ProcessMessage(object source, Message message)
		{
			Engine engine = new Engine();
			engine.SetValue("message", message);
			engine.Execute(Script);
		}
	}
}