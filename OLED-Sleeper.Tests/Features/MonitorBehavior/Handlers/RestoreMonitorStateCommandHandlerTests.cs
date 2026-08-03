using Moq;
using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBehavior.Handlers;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorDimming.Commands;

namespace OLED_Sleeper.Tests.Features.MonitorBehavior.Handlers
{
    public class RestoreMonitorStateCommandHandlerTests
    {
        private const string HardwareId = "MON-123";

        private readonly Mock<IMediator> _mediatorMock;
        private readonly RestoreMonitorStateCommandHandler _handler;

        public RestoreMonitorStateCommandHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _handler = new RestoreMonitorStateCommandHandler(_mediatorMock.Object);
        }

        [Fact]
        public async Task HandleAsync_SendsHideOverlayAndUndimForTheMonitor()
        {
            // Arrange
            var command = new RestoreMonitorStateCommand { HardwareId = HardwareId };

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _mediatorMock.Verify(m => m.SendAsync(It.Is<HideBlackoutOverlayCommand>(c => c.HardwareId == HardwareId)), Times.Once);
            _mediatorMock.Verify(m => m.SendAsync(It.Is<ApplyUndimCommand>(c => c.HardwareId == HardwareId)), Times.Once);
            _mediatorMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleAsync_WhenHideOverlayFails_StillSendsUndim()
        {
            // Arrange
            var command = new RestoreMonitorStateCommand { HardwareId = HardwareId };

            _mediatorMock
                .Setup(m => m.SendAsync(It.IsAny<HideBlackoutOverlayCommand>()))
                .ThrowsAsync(new InvalidOperationException("Overlay teardown failed."));

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(command));

            // Assert
            Assert.Null(exception);
            _mediatorMock.Verify(m => m.SendAsync(It.Is<ApplyUndimCommand>(c => c.HardwareId == HardwareId)), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenUndimFails_DoesNotThrow()
        {
            // Arrange
            var command = new RestoreMonitorStateCommand { HardwareId = HardwareId };

            _mediatorMock
                .Setup(m => m.SendAsync(It.IsAny<ApplyUndimCommand>()))
                .ThrowsAsync(new InvalidOperationException("Brightness restore failed."));

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(command));

            // Assert
            Assert.Null(exception);
        }
    }
}
