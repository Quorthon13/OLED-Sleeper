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
        public async Task HandleAsync_WhenDdcCiSupportedAndLowerBrightnessRequested_CallsOverlayAndDimming()
        {
            // Arrange
            var hardwareId = "MON-123";
            var command = new ApplyBlackoutOverlayCommand { HardwareId = hardwareId, LowerBrightness = true };

            var monitors = new List<MonitorInfo>
            {
                new MonitorInfo { HardwareId = hardwareId, Capabilities = new DdcCiCapabilities(true, 100), Bounds = new Rect(0, 0, 1920, 1080) }
            };

            SetupMonitors(monitors);

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
        public async Task HandleAsync_WhenLowerBrightnessNotRequested_CallsOverlayOnly()
        {
            // Arrange
            var hardwareId = "MON-123";
            var command = new ApplyBlackoutOverlayCommand { HardwareId = hardwareId };

            var monitors = new List<MonitorInfo>
            {
                new MonitorInfo { HardwareId = hardwareId, Capabilities = new DdcCiCapabilities(true, 100), Bounds = new Rect(0, 0, 1920, 1080) }
            };

            SetupMonitors(monitors);

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
        public async Task HandleAsync_WhenDdcCiNotSupported_CallsOverlayOnly()
        {
            // Arrange
            var hardwareId = "MON-123";
            var command = new ApplyBlackoutOverlayCommand { HardwareId = hardwareId };

            var monitors = new List<MonitorInfo>
            {
                new MonitorInfo { HardwareId = hardwareId, Capabilities = new DdcCiCapabilities(false, 0), Bounds = new Rect(0, 0, 1920, 1080) }
            };

            SetupMonitors(monitors);

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

            SetupMonitors(monitors);

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
                new MonitorInfo { HardwareId = hardwareId, Capabilities = new DdcCiCapabilities(false, 0), Bounds = new Rect(0, 0, 1920, 1080) }
            };

            SetupMonitors(monitors);

            _monitorBlackoutServiceMock
                .Setup(x => x.ShowBlackoutOverlayAsync(It.IsAny<string>(), It.IsAny<Rect>()))
                .ThrowsAsync(new Exception("Simulated service failure."));

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(command));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task HandleAsync_WhenMonitorScanFails_CatchesExceptionAndDoesNotThrow()
        {
            // Arrange
            var command = new ApplyBlackoutOverlayCommand { HardwareId = "MON-123" };

            _monitorInfoManagerMock
                .Setup(m => m.GetCurrentMonitorsAsync())
                .ThrowsAsync(new InvalidOperationException("Native monitor enumeration failed."));

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(command));

            // Assert
            Assert.Null(exception);
            _monitorBlackoutServiceMock.Verify(x => x.ShowBlackoutOverlayAsync(It.IsAny<string>(), It.IsAny<Rect>()), Times.Never);
            _monitorDimmingServiceMock.Verify(x => x.DimMonitorAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Makes GetCurrentMonitorsAsync() return the supplied monitor list.
        /// </summary>
        private void SetupMonitors(List<MonitorInfo> monitors)
        {
            _monitorInfoManagerMock
                .Setup(m => m.GetCurrentMonitorsAsync())
                .ReturnsAsync(monitors);
        }
    }
}