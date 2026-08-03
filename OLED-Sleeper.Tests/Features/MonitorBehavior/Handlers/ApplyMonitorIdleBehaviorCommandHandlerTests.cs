using Moq;
using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBehavior.Handlers;
using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Features.MonitorIdleDetection.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorBehavior.Handlers
{
    public class ApplyMonitorIdleBehaviorCommandHandlerTests
    {
        private const string HardwareId = "MON-123";

        private readonly Mock<IMediator> _mediatorMock;
        private readonly ApplyMonitorIdleBehaviorCommandHandler _handler;

        public ApplyMonitorIdleBehaviorCommandHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _handler = new ApplyMonitorIdleBehaviorCommandHandler(_mediatorMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenBehaviorIsBlackout_SendsApplyBlackoutOverlayCommand()
        {
            // Arrange
            var command = new ApplyMonitorIdleBehaviorCommand(CreateEventArgs(MonitorBehaviorType.Blackout));

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _mediatorMock.Verify(m => m.SendAsync(It.Is<ApplyBlackoutOverlayCommand>(c => c.HardwareId == HardwareId)), Times.Once);
            _mediatorMock.Verify(m => m.SendAsync(It.IsAny<ApplyDimCommand>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WhenBehaviorIsDim_SendsApplyDimCommand()
        {
            // Arrange
            var command = new ApplyMonitorIdleBehaviorCommand(CreateEventArgs(MonitorBehaviorType.Dim, dimLevel: 20));

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _mediatorMock.Verify(m => m.SendAsync(It.Is<ApplyDimCommand>(c => c.HardwareId == HardwareId && c.DimLevel == 20)), Times.Once);
            _mediatorMock.Verify(m => m.SendAsync(It.IsAny<ApplyBlackoutOverlayCommand>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WhenDimLevelIsFractional_TruncatesTowardZero()
        {
            // Arrange
            var command = new ApplyMonitorIdleBehaviorCommand(CreateEventArgs(MonitorBehaviorType.Dim, dimLevel: 42.9));

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _mediatorMock.Verify(m => m.SendAsync(It.Is<ApplyDimCommand>(c => c.DimLevel == 42)), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenBehaviorIsNone_SendsNothing()
        {
            // Arrange
            var command = new ApplyMonitorIdleBehaviorCommand(CreateEventArgs(MonitorBehaviorType.None));

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _mediatorMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleAsync_WhenDispatchedCommandFails_PropagatesException()
        {
            // Arrange
            var command = new ApplyMonitorIdleBehaviorCommand(CreateEventArgs(MonitorBehaviorType.Blackout));

            _mediatorMock
                .Setup(m => m.SendAsync(It.IsAny<ApplyBlackoutOverlayCommand>()))
                .ThrowsAsync(new InvalidOperationException("No handler registered."));

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(command));

            // Assert
            Assert.IsType<InvalidOperationException>(exception);
        }

        /// <summary>
        /// Builds idle event args for a monitor configured with the given behavior and dim level.
        /// </summary>
        private static MonitorIdleStateEventArgs CreateEventArgs(MonitorBehaviorType behavior, double dimLevel = 15)
        {
            var settings = new MonitorSettings
            {
                HardwareId = HardwareId,
                IsManaged = true,
                Behavior = behavior,
                DimLevel = dimLevel
            };

            return new MonitorIdleStateEventArgs(
                HardwareId,
                displayNumber: 1,
                bounds: new Rect(0, 0, 1920, 1080),
                settings,
                foregroundWindowHandle: 0,
                reason: ActivityReason.None);
        }
    }
}
