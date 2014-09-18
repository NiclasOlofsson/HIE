using System.IO;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core.Model
{
	[TestClass]
	public class Hl7DisassemblerTest
	{
		[TestMethod]
		public void DisassembleTest()
		{
			Hl7Disassembler disassembler = new Hl7Disassembler();

			string filePath = "Hl7DisassemblerTest-hl7.txt";

			byte[] data = null;
			using (StreamReader reader = new StreamReader(filePath))
			{
				string text = reader.ReadToEnd();
				data = Encoding.UTF8.GetBytes(text);
			}

			disassembler.Disassemble(data);

			Message message = disassembler.NextMessage();
			Assert.IsNotNull(message);

			Assert.IsNull(disassembler.NextMessage(), "Expected only one message from pipeline");

			XDocument document = XDocument.Load("Hl7DisassemblerTest-hl7.xml");
			Assert.IsTrue(XNode.DeepEquals(document, message.RetrieveAs<XDocument>()));
		}
	}
}
