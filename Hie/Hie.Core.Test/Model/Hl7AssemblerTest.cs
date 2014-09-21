using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core.Model
{
	[TestClass]
	public class Hl7AssemblerTest
	{
		[TestMethod]
		public void AssembleTest()
		{
			XDocument document = XDocument.Load("Hl7DisassemblerTest-hl7.xml");

			Message message = new Message("");
			message.SetValueFrom(document);

			Hl7Assembler assembler = new Hl7Assembler();
			assembler.AddMessage(message);
			byte[] result = assembler.Assemble();
			Assert.IsNotNull(result);

			byte[] expected;
			using (StreamReader reader = new StreamReader("Hl7DisassemblerTest-hl7-2.txt"))
			{
				string text = reader.ReadToEnd();
				expected = Encoding.UTF8.GetBytes(text);
			}

			Assert.IsTrue(expected.SequenceEqual(result));
		}

		[TestMethod]
		public void XmlToHl7ConverterTest()
		{
			XDocument document = XDocument.Load("Hl7DisassemblerTest-hl7.xml");

			var element = document.Descendants("MSH.19.1").FirstOrDefault();
			Assert.IsNotNull(element);
			Assert.AreEqual(" ", element.Value, "We need these empty spaces in the file to make sure the XML framework doesn't throw them away on the way");

			String segmentSeparator = "\r";
			String fieldSeparator = "|";
			String componentSeparator = "^";
			String repetitionSeparator = "~";
			String subcomponentSeparator = "&";
			String escapeCharacter = "\\";

			var converter = new XmlToHl7Converter(segmentSeparator, fieldSeparator, componentSeparator, repetitionSeparator, escapeCharacter, subcomponentSeparator, true);
			byte[] result = converter.Convert(document);
			Assert.IsNotNull(result);

			string outFilePath = "hl7-assembler-test-out.txt";
			using (StreamWriter writer = new StreamWriter(outFilePath))
			{
				writer.Write(Encoding.UTF8.GetString(result));
				writer.Close();
			}

			byte[] expected = null;
			using (StreamReader reader = new StreamReader("Hl7DisassemblerTest-hl7-2.txt"))
			{
				string text = reader.ReadToEnd();
				expected = Encoding.UTF8.GetBytes(text);
			}

			Assert.AreEqual(expected[0], result[0]);
			Assert.AreEqual(expected[1], result[1]);
			Assert.AreEqual(expected[expected.Length - 5], result[expected.Length - 5], "Failed with | and \" differ at end of the second segment");
			Assert.AreEqual(expected.Length, result.Length);
			Assert.IsTrue(expected.SequenceEqual(result), "Expected and result message didn't match.");
		}
	}
}
