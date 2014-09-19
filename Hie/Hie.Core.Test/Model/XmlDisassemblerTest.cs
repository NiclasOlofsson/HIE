using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core.Model
{
	[TestClass]
	public class XmlDisassemblerTest
	{
		[TestMethod]
		public void DisassembleXmlFromEndpointTest()
		{
			// Goal: Send in bytes (from stream) and get an XML document out
			var disassembler = new XmlDisassembler();

			XDocument document = new XDocument(new XElement("body",
				new XElement("level1",
					new XElement("level2", "text"),
					new XElement("level2", "other text"))));
			document.Declaration = new XDeclaration("1.0", "UTF-8", "yes");

			byte[] data = Encoding.GetEncoding(document.Declaration.Encoding).GetBytes(document.ToString());

			disassembler.Disassemble(data);
			Message message = disassembler.NextMessage();

			Assert.IsNull(disassembler.NextMessage(), "Expected only one message back");

			Assert.IsNotNull(message);
			Assert.IsNotNull(message.Stream);
			Assert.IsTrue(XNode.DeepEquals(document, XDocument.Parse(message.GetString())));
			Assert.IsTrue(XNode.DeepEquals(document, XDocument.Load(message.GetStream())));

			// Move these to MessageTest instead
			Assert.IsTrue(XNode.DeepEquals(document, message.RetrieveAs<XDocument>()));
			Assert.IsTrue(XNode.DeepEquals(document, message.RetrieveAs<XNode>()));
			Assert.IsTrue(XNode.DeepEquals(document, XDocument.Parse(message.RetrieveAs<XmlDocument>().OuterXml)));
		}
	}
}
