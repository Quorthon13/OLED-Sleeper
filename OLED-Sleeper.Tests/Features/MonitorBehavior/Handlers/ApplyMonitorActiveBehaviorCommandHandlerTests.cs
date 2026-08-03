using Moq;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBehavior.Handlers;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Messaging.Interfaces;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorBehavior.Handlers
{
    public class ApplyMonitorActiveBehaviorCommandHandlerTests
    {
        private const string HardwareId = "MON-123";
        private const nint OverlayHandle = 0x1234;
        private const nint ForeignHandle = 0x5678;

        private readonly Mock<IMonitorBlackoutService> _monitorBlackoutServiceMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly ApplyMonitorActiveBehaviorCommandHandler _handler;

        public ApplyMonitorActiveBehaviorCommandHandlerTests()
        {
            _monitorBlackoutServiceMock = new Mock<IMonitorBlackoutService>();
            _mediatorMock = new Mock<IMediator>();

            _handler = new ApplyMonitorActiveBehaviorCommandHandler(
                _monitorBlackoutServiceMock.Object,
                _mediatorMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenActiveWindowIsOverlay_FlagsIgnoredAndDoesNotRestore()
        {
            // Arrange
            var eventArgs = CreateEventArgs(ActivityReason.ActiveWindow, OverlayHandle);
            SetupOverlayHandle(OverlayHandle);

            // Act
            await _handler.HandleAsync(new ApplyMonitorActiveBehaviorCommand(eventArgs));

            // Assert
            Assert.True(eventArgs.IsIgnored);
            _mediatorMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleAsync_WhenActiveWindowIsNotOverlay_SendsRestoreMonitorStateCommand()
        {
            // Arrange
            var eventArgs = CreateEventArgs(ActivityReason.ActiveWindow, ForeignHandle);
            SetupOverlayHandle(OverlayHandle);

            // Act
            await _handler.HandleAsync(new ApplyMonitorActiveBehaviorCommand(eventArgs));

            // Assert
            Assert.False(eventArgs.IsIgnored);
            _mediatorMock.Verify(m => m.SendAsync(It.Is<RestoreMonitorStateCommand>(c => c.HardwareId == HardwareId)), Times.Once);
        }

        [Theory]
        [InlineData(ActivityReason.MousePosition)]
        [InlineData(ActivityReason.SystemInput)]
        [InlineData(ActivityReason.None)]
        public async Task HandleAsync_WhenReasonIsNotActiveWindow_RestoresEvenIfForegroundWindowIsOverlay(ActivityReason reason)
        {
            // Arrange
            var eventArgs = CreateEventArgs(reason, OverlayHandle);
            SetupOverlayHandle(OverlayHandle);

            // Act
            await _handler.HandleAsync(new ApplyMonitorActiveBehaviorCommand(eventArgs));

            // Assert
            Assert.False(eventArgs.IsIgnored);
            _mediatorMock.Verify(m => m.SendAsync(It.Is<RestoreMonitorStateCommand>(c => c.HardwareId == HardwareId)), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenRestoreFails_DoesNotThrow()
        {
            // Arrange
            var eventArgs = CreateEventArgs(ActivityReason.SystemInput, ForeignHandle);

            _mediatorMock
                .Setup(m => m.SendAsync(It.IsAny<RestoreMonitorStateCommand>()))
                .ThrowsAsync(new InvalidOperationException("Restore failed."));

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(new ApplyMonitorActiveBehaviorCommand(eventArgs)));

            // Assert
            Assert.Null(exception);
            Assert.False(eventArgs.IsIgnored);
        }

        /// <summary>
        /// Makes IsOverlayWindow answer true only for the given handle.
        /// </summary>
        private void SetupOverlayHandle(nint overlayHandle)
        {
            _monitorBlackoutServiceMock
                .Setup(x => x.IsOverlayWindow(It.IsAny<nint>()))
                .Returns<nint>(handle => handle == overlayHandle);
        }

        /// <summary>
        /// Builds active event args for the given activity reason and foreground window handle.
        /// </summary>
        private static MonitorIdleStateEventArgs CreateEventArgs(ActivityReason reason, nint foregroundWindowHandle)
        {
            var settings = new MonitorSettings { HardwareId = HardwareId, IsManaged = true };

            return new MonitorIdleStateEventArgs(
                HardwareId,
                displayNumber: 1,
                bounds: new Rect(0, 0, 1920, 1080),
                settings,
                foregroundWindowHandle,
                reason);
        }
    }
}
