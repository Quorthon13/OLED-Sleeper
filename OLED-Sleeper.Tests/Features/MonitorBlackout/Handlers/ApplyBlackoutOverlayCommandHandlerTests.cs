using Moq;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Handlers;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorBlackout.Handlers
{
    public class ApplyBlackoutOverlayCommandHandlerTests
    {
        private readonly Mock<IMonitorInfoManager> _monitorInfoManagerMock;
        private readonly Mock<IMonitorBlackoutService> _monitorBlackoutServiceMock;
        private readonly Mock<IMonitorDimmingService> _monitorDimmingServiceMock;
        private readonly ApplyBlackoutOverlayCommandHandler _handler;

        public ApplyBlackoutOverlayCommandHandlerTests()
        {
            _monitorInfoManagerMock = new Mock<IMonitorInfoManager>();
            _monitorBlackoutServiceMock = new Mock<IMonitorBlackoutService>();
            _monitorDimmingServiceMock = new Mock<IMonitorDimmingService>();

            _handler = new ApplyBlackoutOverlayCommandHandler(
                _monitorInfoManagerMock.Object,
                _monitorBlackoutServiceMock.Object,
                _monitorDimmingServiceMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenDdcCiSupported_CallsOverlayAndDimming()
        {
            // Arrange
            var hardwareId = "MON-123";
            var command = new ApplyBlackoutOverlayCommand { HardwareId = hardwareId };

            var monitors = new List<MonitorInfo>
            {
                new MonitorInfo { HardwareId = hardwareId, IsDdcCiSupported = true, Bounds = new Rect(0, 0, 1920, 1080) }
            };

            SetupMonitorListReadyEvent(monitors);

            _monitorBlackoutServiceMock
                .Setup(x => x.ShowBlackoutOverlayAsync(It.IsAny<string>(), It.IsAny<Rect>()))
                .Returns(Task.CompletedTask);

            _monitorDimmingServiceMock
                .Setup(x => x.DimMonitorAsync(hardwareId, 0))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _monitorBlackoutServiceMock.Verify(x => x.ShowBlackoutOverlayAsync(hardwareId, It.IsAny<Rect>()), Times.Once);
            _monitorDimmingServiceMock.Verify(x => x.DimMonitorAsync(hardwareId, 0), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenDdcCiNotSupported_CallsOverlayOnly()
        {
            // Arrange
            var hardwareId = "MON-123";
            var command = new ApplyBlackoutOverlayCommand { HardwareId = hardwareId };

            var monitors = new List<MonitorInfo>
            {
                new MonitorInfo { HardwareId = hardwareId, IsDdcCiSupported = false, Bounds = new Rect(0, 0, 1920, 1080) }
            };

            SetupMonitorListReadyEvent(monitors);

            _monitorBlackoutServiceMock
                .Setup(x => x.ShowBlackoutOverlayAsync(It.IsAny<string>(), It.IsAny<Rect>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _monitorBlackoutServiceMock.Verify(x => x.ShowBlackoutOverlayAsync(hardwareId, It.IsAny<Rect>()), Times.Once);
            _monitorDimmingServiceMock.Verify(x => x.DimMonitorAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WhenMonitorNotFound_CatchesExceptionAndDoesNotThrow()
        {
            // Arrange
            var hardwareId = "MON-UNKNOWN";
            var command = new ApplyBlackoutOverlayCommand { HardwareId = hardwareId };
            var monitors = new List<MonitorInfo>();

            SetupMonitorListReadyEvent(monitors);

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(command));

            // Assert
            Assert.Null(exception);
            _monitorBlackoutServiceMock.Verify(x => x.ShowBlackoutOverlayAsync(It.IsAny<string>(), It.IsAny<Rect>()), Times.Never);
            _monitorDimmingServiceMock.Verify(x => x.DimMonitorAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WhenOverlayServiceThrows_CatchesExceptionAndDoesNotThrow()
        {
            // Arrange
            var hardwareId = "MON-123";
            var command = new ApplyBlackoutOverlayCommand { HardwareId = hardwareId };

            var monitors = new List<MonitorInfo>
            {
                new MonitorInfo { HardwareId = hardwareId, IsDdcCiSupported = false, Bounds = new Rect(0, 0, 1920, 1080) }
            };

            SetupMonitorListReadyEvent(monitors);

            _monitorBlackoutServiceMock
                .Setup(x => x.ShowBlackoutOverlayAsync(It.IsAny<string>(), It.IsAny<Rect>()))
                .ThrowsAsync(new Exception("Simulated service failure."));

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(command));

            // Assert
            Assert.Null(exception);
        }

        /// <summary>
        /// Simulates the firing of the MonitorListReady event immediately when GetCurrentMonitorsAsync() is called.
        /// </summary>
        private void SetupMonitorListReadyEvent(List<MonitorInfo> monitors)
        {
            _monitorInfoManagerMock
                .Setup(m => m.GetCurrentMonitorsAsync())
                .Callback(() =>
                {
                    _monitorInfoManagerMock.Raise(m => m.MonitorListReady += null, _monitorInfoManagerMock.Object, monitors);
                });
        }
    }
}