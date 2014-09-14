using Hie.Core.Model;
using Jint;

namespace Hie.Core.Modules.JavaScript
{
	public class JavaScriptFilter : IFilter
	{
		public string Script { get; set; }

		public bool Evaluate(object source, Message message)
		{
			Engine engine = new Engine();
			engine.SetValue("message", message);
			engine.Execute(Script);

			if (!engine.GetCompletionValue().IsBoolean())
			{
				return false;
			}

			return engine.GetCompletionValue().AsBoolean();
		}
	}
}
