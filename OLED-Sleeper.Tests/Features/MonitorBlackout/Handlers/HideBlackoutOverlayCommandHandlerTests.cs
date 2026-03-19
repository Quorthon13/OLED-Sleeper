using Moq;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Handlers;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;

namespace OLED_Sleeper.Tests.Features.MonitorBlackout.Handlers
{
    public class HideBlackoutOverlayCommandHandlerTests
    {
        private readonly Mock<IMonitorBlackoutService> _monitorBlackoutServiceMock;
        private readonly HideBlackoutOverlayCommandHandler _handler;

        public HideBlackoutOverlayCommandHandlerTests()
        {
            _monitorBlackoutServiceMock = new Mock<IMonitorBlackoutService>();
            _handler = new HideBlackoutOverlayCommandHandler(_monitorBlackoutServiceMock.Object);
        }

        [Fact]
        public async Task HandleAsync_CallsHideBlackoutOverlayAsync()
        {
            // Arrange
            var hardwareId = "MON-123";
            var command = new HideBlackoutOverlayCommand { HardwareId = hardwareId };
            _monitorBlackoutServiceMock
                .Setup(x => x.HideBlackoutOverlayAsync(hardwareId))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _monitorBlackoutServiceMock.Verify(x => x.HideBlackoutOverlayAsync(hardwareId), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenServiceThrows_CatchesExceptionAndDoesNotThrow()
        {
            // Arrange
            var hardwareId = "MON-123";
            var command = new HideBlackoutOverlayCommand { HardwareId = hardwareId };
            _monitorBlackoutServiceMock
                .Setup(x => x.HideBlackoutOverlayAsync(hardwareId))
                .ThrowsAsync(new Exception("Simulated failure"));

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(command));

            // Assert
            Assert.Null(exception);
            _monitorBlackoutServiceMock.Verify(x => x.HideBlackoutOverlayAsync(hardwareId), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithNullHardwareId_DoesNotThrow()
        {
            // Arrange
            var command = new HideBlackoutOverlayCommand { HardwareId = null };

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(command));

            // Assert
            Assert.Null(exception);
            _monitorBlackoutServiceMock.Verify(x => x.HideBlackoutOverlayAsync(It.IsAny<string>()), Times.Once);
        }
    }
}