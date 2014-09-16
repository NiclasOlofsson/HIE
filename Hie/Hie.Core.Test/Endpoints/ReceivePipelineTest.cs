using Hie.Core.Mocks;
using Hie.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hie.Core.Endpoints
{
	// A receive pipleline is responsible for the following:
	// - decode (mime, etc)
	// - disassemble messages (HL7 to XML)
	// - validate (XSD)

	[TestClass]
	public class ReceivePipelineTest
	{
		[TestMethod]
		public void CreateGeneralComponentTest()
		{
			IPipelineComponent component = new PipelineComponentMock();

			IOptions options = new Mock<IOptions>().Object;
			component.Initialize(options);
		}

		[TestMethod]
		public void CreateEncoderTest()
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
		public void CreatePipelineManagerTest()
		{
			var applicationHost = new Mock<IApplicationHost>();
			IPipelineManager manager = new PipelineManager(applicationHost.Object);

			// Register a pipeline component with an endpoint in the manager
			var endpoint = new Mock<IEndpoint>();
			var decoderMock = new Mock<IPipelineComponent>().As<IDecoder>();
			var disassemblerMock = new Mock<IPipelineComponent>().As<IDisassembler>();
			manager.AddPipelineComponent(endpoint.Object, decoderMock.Object);
			manager.AddPipelineComponent(endpoint.Object, disassemblerMock.Object);

			byte[] data = new byte[0];
			decoderMock.SetupSequence(decoder => decoder.Decode(It.IsAny<byte[]>()))
				.Returns(new byte[] {0x00})
				.Returns(new byte[] {0x01})
				.Returns(new byte[] {0x02})
				.Returns(new byte[] {0x03})
				;

			// Push some data to the manager from the endpoint
			manager.PushPipelineData(endpoint.Object, data);
			manager.PushPipelineData(endpoint.Object, data);
			manager.PushPipelineData(endpoint.Object, data);
			manager.PushPipelineData(endpoint.Object, data);

			decoderMock.Verify(decoder => decoder.Decode(data), Times.Exactly(4));

			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x00)), Times.Once);
			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x01)), Times.Once);
			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x02)), Times.Once);
			disassemblerMock.Verify(disassembler => disassembler.Disassemble(It.Is<byte[]>(bytes => bytes[0] == 0x03)), Times.Once);

			applicationHost.Verify(host => host.PublishMessage(It.IsAny<IEndpoint>(), It.IsAny<Message>()), Times.Exactly(4));
		}
	}
}
