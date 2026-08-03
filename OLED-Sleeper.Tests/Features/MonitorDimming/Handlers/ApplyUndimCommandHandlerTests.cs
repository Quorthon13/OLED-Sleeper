using Moq;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Features.MonitorDimming.Handlers;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;

namespace OLED_Sleeper.Tests.Features.MonitorDimming.Handlers
{
    public class ApplyUndimCommandHandlerTests
    {
        private const string HardwareId = "MON-123";

        private readonly Mock<IMonitorDimmingService> _monitorDimmingServiceMock;
        private readonly ApplyUndimCommandHandler _handler;

        public ApplyUndimCommandHandlerTests()
        {
            _monitorDimmingServiceMock = new Mock<IMonitorDimmingService>();
            _handler = new ApplyUndimCommandHandler(_monitorDimmingServiceMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenCommandIsHandled_UndimsTheMonitor()
        {
            // Arrange
            var command = new ApplyUndimCommand { HardwareId = HardwareId };

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _monitorDimmingServiceMock.Verify(x => x.UndimMonitorAsync(HardwareId), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenTheServiceThrows_SwallowsTheException()
        {
            // Arrange
            var command = new ApplyUndimCommand { HardwareId = HardwareId };
            _monitorDimmingServiceMock
                .Setup(x => x.UndimMonitorAsync(HardwareId))
                .ThrowsAsync(new Exception("Simulated failure"));

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(command));

            // Assert
            Assert.Null(exception);
            _monitorDimmingServiceMock.Verify(x => x.UndimMonitorAsync(HardwareId), Times.Once);
        }
    }
}
