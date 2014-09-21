using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hie.Core.Model
{
	// A receive pipleline is responsible for the following:
	// - decode (mime, etc)
	// - disassemble messages (HL7 to XML)
	// - validate (XSD)

	[TestClass]
	public class PipelineTest
	{
		[TestMethod]
		public void PipelineProcessingTest()
		{
			var appHostMock = new Mock<ApplicationHost>();
			appHostMock.CallBase = true;
			var appHost = appHostMock.Object;

			var receiveEndpoint = new Mock<IEndpoint>();
			var sendEndpoint = new Mock<IEndpoint>();
			var decoderMock = new Mock<IPipelineComponent>().As<IDecoder>();
			var encoderMock = new Mock<IPipelineComponent>().As<IEncoder>();
			var disassemblerMock = new Mock<IPipelineComponent>().As<IDisassembler>();
			var assemblerMock = new Mock<IPipelineComponent>().As<IAssembler>();

			appHostMock.Setup(host => host.PublishMessage(It.IsNotNull<object>(), It.IsNotNull<Message>()));

			// Receive decoder and disassembler
			appHost.AddPipelineComponent(receiveEndpoint.Object, decoderMock.Object);
			appHost.AddPipelineComponent(receiveEndpoint.Object, disassemblerMock.Object);

			// Send assembler and encoder
			appHost.AddPipelineComponent(sendEndpoint.Object, assemblerMock.Object);
			appHost.AddPipelineComponent(sendEndpoint.Object, encoderMock.Object);

			// Setup pipeline to produce data
			decoderMock.SetupSequence(decoder => decoder.Decode(It.IsAny<byte[]>()))
				.Returns(new byte[] { 0x00 })
				.Returns(new byte[] { 0x01 })
				.Returns(new byte[] { 0x02 })
				.Returns(new byte[] { 0x03 })
				;

			assemblerMock.SetupSequence(assembler => assembler.Assemble())
				.Returns(new byte[] { 0x00 })
				.Returns(new byte[] { 0x01 })
				.Returns(new byte[] { 0x02 })
				.Returns(new byte[] { 0x03 })
				;

			disassemblerMock.SetupSequence(disassembler => disassembler.NextMessage())
				.Returns(new Message("").SetValueFrom("1"))
				.Returns(new Message("").SetValueFrom("2"))
				.Returns(new Message("").SetValueFrom("3"))
				.Returns(new Message("").SetValueFrom("4"))
				;

			// Receive pipeline tests

			byte[] data = new byte[0];
			appHost.PushPipelineData(receiveEndpoint.Object, data);
			appHost.PushPipelineData(receiveEndpoint.Object, data);
			appHost.PushPipelineData(receiveEndpoint.Object, data);
			appHost.PushPipelineData(receiveEndpoint.Object, data);

			decoderMock.Verify(decoder => decoder.Decode(data), Times.Exactly(4));

			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.IsAny<byte[]>()), Times.Exactly(4));
			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x00)), Times.Once);
			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x01)), Times.Once);
			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x02)), Times.Once);
			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x03)), Times.Once);

			appHostMock.Verify(host => host.PublishMessage(It.IsAny<IEndpoint>(), It.IsAny<Message>()), Times.Exactly(4));
			appHostMock.Verify(host => host.PublishMessage(It.IsAny<IEndpoint>(), It.Is<Message>(msg => msg.GetString(null).Equals("1"))), Times.Once);
			appHostMock.Verify(host => host.PublishMessage(It.IsAny<IEndpoint>(), It.Is<Message>(msg => msg.GetString(null).Equals("2"))), Times.Once);
			appHostMock.Verify(host => host.PublishMessage(It.IsAny<IEndpoint>(), It.Is<Message>(msg => msg.GetString(null).Equals("3"))), Times.Once);
			appHostMock.Verify(host => host.PublishMessage(It.IsAny<IEndpoint>(), It.Is<Message>(msg => msg.GetString(null).Equals("4"))), Times.Once);

			// Send pipeline tests

			Message message = new Message("nothing");
			appHost.PushPipelineData(sendEndpoint.Object, message.Clone());
			appHost.PushPipelineData(sendEndpoint.Object, message.Clone());
			appHost.PushPipelineData(sendEndpoint.Object, message.Clone());
			appHost.PushPipelineData(sendEndpoint.Object, message.Clone());

			assemblerMock.Verify(assembler => assembler.AddMessage(It.IsNotNull<Message>()), Times.Exactly(4));

			encoderMock.Verify(encoder => encoder.Encode(It.IsAny<byte[]>()), Times.Exactly(4));
			encoderMock.Verify(encoder => encoder.Encode(It.Is<byte[]>(bytes => bytes[0] == 0x00)), Times.Once);
			encoderMock.Verify(encoder => encoder.Encode(It.Is<byte[]>(bytes => bytes[0] == 0x01)), Times.Once);
			encoderMock.Verify(encoder => encoder.Encode(It.Is<byte[]>(bytes => bytes[0] == 0x02)), Times.Once);
			encoderMock.Verify(encoder => encoder.Encode(It.Is<byte[]>(bytes => bytes[0] == 0x03)), Times.Once);

			// sendEndpointMock.Verify( verify that it receive data from the pipeline for sending )
		}
	}
}
