using System.Xml.Linq;
using Hie.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core.Modules.JavaScript
{
	[TestClass()]
	public class JavaScriptTransformerTest
	{
		[TestMethod()]
		public void ProcessMessageTest()
		{
			string fileInputPath = "Hl7DisassemblerTest-hl7.xml";
			var input = XDocument.Load(fileInputPath);

			Message message = new Message("");
			message.SetValueFrom(input);

			JavaScriptTransformer transformer = new JavaScriptTransformer
			{
				Script = @"
if(msg['MSH']['MSH.8'] != null) delete msg['MSH']['MSH.8'];
msg['MSH']['MSH.2'] = 'TEST';
"
			};

			// Execute
			transformer.ProcessMessage(null, message);

			// Assert
			XDocument expected = new XDocument(input);
			expected.Descendants("MSH.8").Remove();
			expected.Element("HL7Message").Element("MSH").Element("MSH.2").SetValue("TEST");

			var result = message.RetrieveAs<XDocument>();

			Assert.IsTrue(XNode.DeepEquals(expected, result));
		}
	}
}
