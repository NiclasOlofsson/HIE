using System.IO;
using System.Net;
using Jint;

namespace Hie.Core.Model
{
	public class Filter
	{
		public Filter()
		{
		}

		public virtual bool Evaluate(Message message)
		{
			return true;
		}
	}


	public class DelegateFilter : Filter
	{
		public delegate bool FilterProcessor(Message message);

		private FilterProcessor _processor;

		public DelegateFilter()
		{
		}

		public DelegateFilter(FilterProcessor processor)
		{
			_processor = processor;
		}

		public override bool Evaluate(Message message)
		{
			return _processor(message);
		}
	}

	public class JavaScriptFilter : Filter
	{
		private string _json2;

		public string Script { get; set; }

		public JavaScriptFilter()
		{
		}

		public override bool Evaluate(Message message)
		{
			Engine engine = new Engine();
			engine.SetValue("message", message);

			if (_json2 == null)
			{
				WebRequest wr = HttpWebRequest.Create("https://raw.githubusercontent.com/douglascrockford/JSON-js/master/json2.js");
				WebResponse r = wr.GetResponse();
				TextReader tr = new StreamReader(r.GetResponseStream());

				_json2 = tr.ReadToEnd();
			}

			engine
				.Execute(_json2)
				.Execute(Script);

			if (engine.GetCompletionValue().IsString())
			{
				return false;
			}
			else
			{
				return (bool) engine.GetCompletionValue().AsBoolean();
			}
		}
	}
}
