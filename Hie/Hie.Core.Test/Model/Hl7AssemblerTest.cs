using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core.Model
{
	[TestClass]
	public class Hl7AssemblerTest
	{
		[TestMethod]
		[Ignore]
		public void AssembleTest()
		{
			//Hl7Disassembler disassembler = new Hl7Disassembler();

			//string inputFilePath = "Hl7DisassemblerTest-hl7.xml";
			//string outputfilePath = "Hl7DisassemblerTest-hl7.txt";

			//byte[] data = null;
			//using (StreamReader reader = new StreamReader(inputFilePath))
			//{
			//	string text = reader.ReadToEnd();
			//	data = Encoding.UTF8.GetBytes(text);
			//}

			//disassembler.Disassemble(data);

			//Message message = disassembler.NextMessage();
			//Assert.IsNotNull(message);

			//Assert.IsNull(disassembler.NextMessage(), "Expected only one message from pipeline");

			//XDocument document = XDocument.Load(outputfilePath);
			//Assert.IsTrue(XNode.DeepEquals(document, message.RetrieveAs<XDocument>()));
		}
	}
}
