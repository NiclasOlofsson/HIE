using Hie.Core.Mocks;
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
		public void CreateGeneralComponentTest()
		{
			IPipelineComponent component = new PipelineComponentMock();

			IOptions options = new Mock<IOptions>().Object;
			component.Initialize(options);
		}

		[TestMethod]
		public void CreateDecoderTest()
		{
			IDecoder decoder = new PipelineComponentMock();

			IOptions options = new Mock<IOptions>().Object;
			decoder.Initialize(options);

			byte[] data = new byte[0];
			data = decoder.Decode(data);
		}

		[TestMethod]
		public void CreateDisassemblerTest()
		{
			IDisassembler dissassembler = new PipelineComponentMock();

			IOptions options = new Mock<IOptions>().Object;
			dissassembler.Initialize(options);

			byte[] data = new byte[0];
			dissassembler.Disassemble(data);
			Message message = dissassembler.NextMessage();
		}


		[TestMethod]
		public void CreateEncoderTest()
		{
			IEncoder encoder = new PipelineComponentMock();

			IOptions options = new Mock<IOptions>().Object;
			encoder.Initialize(options);

			byte[] data = new byte[0];
			data = encoder.Encode(data);
		}

		[TestMethod]
		public void CreatePipelineManagerTest()
		{
			var applicationHost = new Mock<IApplicationHost>();
			IPipelineManager manager = new PipelineManager(applicationHost.Object);

			// Register a pipeline component with an endpoint in the manager
			var receiveEndpoint = new Mock<IEndpoint>();
			var sendEndpoint = new Mock<IEndpoint>();
			var decoderMock = new Mock<IPipelineComponent>().As<IDecoder>();
			var encoderMock = new Mock<IPipelineComponent>().As<IEncoder>();
			var disassemblerMock = new Mock<IPipelineComponent>().As<IDisassembler>();
			var assemblerMock = new Mock<IPipelineComponent>().As<IAssembler>();

			manager.AddPipelineComponent(receiveEndpoint.Object, decoderMock.Object);
			manager.AddPipelineComponent(receiveEndpoint.Object, disassemblerMock.Object);
			manager.AddPipelineComponent(sendEndpoint.Object, encoderMock.Object);
			manager.AddPipelineComponent(sendEndpoint.Object, assemblerMock.Object);

			decoderMock.SetupSequence(decoder => decoder.Decode(It.IsAny<byte[]>()))
				.Returns(new byte[] { 0x00 })
				.Returns(new byte[] { 0x01 })
				.Returns(new byte[] { 0x02 })
				.Returns(new byte[] { 0x03 })
				;

			assemblerMock.SetupSequence(encoder => encoder.Assemble())
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
			manager.PushPipelineData(receiveEndpoint.Object, data);
			manager.PushPipelineData(receiveEndpoint.Object, data);
			manager.PushPipelineData(receiveEndpoint.Object, data);
			manager.PushPipelineData(receiveEndpoint.Object, data);

			decoderMock.Verify(decoder => decoder.Decode(data), Times.Exactly(4));

			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.IsAny<byte[]>()), Times.Exactly(4));
			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x00)), Times.Once);
			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x01)), Times.Once);
			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x02)), Times.Once);
			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x03)), Times.Once);

			applicationHost.Verify(host => host.PublishMessage(It.IsAny<IEndpoint>(), It.IsAny<Message>()), Times.Exactly(4));
			applicationHost.Verify(host => host.PublishMessage(It.IsAny<IEndpoint>(), It.Is<Message>(msg => msg.GetString(null).Equals("1"))), Times.Once);
			applicationHost.Verify(host => host.PublishMessage(It.IsAny<IEndpoint>(), It.Is<Message>(msg => msg.GetString(null).Equals("2"))), Times.Once);
			applicationHost.Verify(host => host.PublishMessage(It.IsAny<IEndpoint>(), It.Is<Message>(msg => msg.GetString(null).Equals("3"))), Times.Once);
			applicationHost.Verify(host => host.PublishMessage(It.IsAny<IEndpoint>(), It.Is<Message>(msg => msg.GetString(null).Equals("4"))), Times.Once);

			// Send pipeline tests

			Message message = new Message("nothing");
			manager.PushPipelineData(sendEndpoint.Object, message.Clone());
			manager.PushPipelineData(sendEndpoint.Object, message.Clone());
			manager.PushPipelineData(sendEndpoint.Object, message.Clone());
			manager.PushPipelineData(sendEndpoint.Object, message.Clone());

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
