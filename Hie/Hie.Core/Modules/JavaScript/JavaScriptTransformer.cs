using Hie.Core.Model;
using Jint;
using Jint.Native;
using Newtonsoft.Json;

namespace Hie.Core.Modules.JavaScript
{
	public class JavaScriptTransformer : ITransformer
	{
		public string Script { get; set; }

		public void ProcessMessage(object source, Message message)
		{
			string jsonScript = "var msg = " + JsonConvert.SerializeXmlNode(message.GetXmlDocument(), Formatting.Indented, true);

			Engine engine = new Engine();
			engine.SetValue("message", message);
			engine.Execute(jsonScript);
			engine.Execute(Script);
			engine.Execute("var jsonMsg = JSON.stringify(msg);");
			JsValue obj = engine.GetValue("jsonMsg");

			string jsonString = obj.AsString();
			var document = JsonConvert.DeserializeXNode(jsonString, "HL7Message");
			message.SetValueFrom(document);
		}
	}
}
