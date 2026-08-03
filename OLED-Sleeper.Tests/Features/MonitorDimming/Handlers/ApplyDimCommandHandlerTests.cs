using Moq;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Features.MonitorDimming.Handlers;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;

namespace OLED_Sleeper.Tests.Features.MonitorDimming.Handlers
{
    public class ApplyDimCommandHandlerTests
    {
        private const string HardwareId = "MON-123";

        private readonly Mock<IMonitorDimmingService> _monitorDimmingServiceMock;
        private readonly ApplyDimCommandHandler _handler;

        public ApplyDimCommandHandlerTests()
        {
            _monitorDimmingServiceMock = new Mock<IMonitorDimmingService>();
            _handler = new ApplyDimCommandHandler(_monitorDimmingServiceMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenCommandIsHandled_DimsTheMonitorAtTheCommandedLevel()
        {
            // Arrange
            var command = new ApplyDimCommand { HardwareId = HardwareId, DimLevel = 15 };

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _monitorDimmingServiceMock.Verify(x => x.DimMonitorAsync(HardwareId, 15), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenTheServiceThrows_SwallowsTheException()
        {
            // Arrange
            var command = new ApplyDimCommand { HardwareId = HardwareId, DimLevel = 15 };
            _monitorDimmingServiceMock
                .Setup(x => x.DimMonitorAsync(HardwareId, 15))
                .ThrowsAsync(new Exception("Simulated failure"));

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(command));

            // Assert
            Assert.Null(exception);
            _monitorDimmingServiceMock.Verify(x => x.DimMonitorAsync(HardwareId, 15), Times.Once);
        }
    }
}
