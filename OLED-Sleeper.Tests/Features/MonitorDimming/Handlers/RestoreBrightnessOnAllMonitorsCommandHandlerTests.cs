using Moq;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Features.MonitorDimming.Handlers;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;

namespace OLED_Sleeper.Tests.Features.MonitorDimming.Handlers
{
    public class RestoreBrightnessOnAllMonitorsCommandHandlerTests
    {
        private const string HardwareId = "MON-123";
        private const string SecondHardwareId = "MON-456";

        private readonly Mock<IMonitorDimmingService> _monitorDimmingServiceMock;
        private readonly RestoreBrightnessOnAllMonitorsCommandHandler _handler;

        public RestoreBrightnessOnAllMonitorsCommandHandlerTests()
        {
            _monitorDimmingServiceMock = new Mock<IMonitorDimmingService>();
            _handler = new RestoreBrightnessOnAllMonitorsCommandHandler(_monitorDimmingServiceMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenNoMonitorIsDimmed_UndimsNothing()
        {
            // Arrange
            SetupDimmedMonitors(new Dictionary<string, uint>());

            // Act
            await _handler.HandleAsync(new RestoreBrightnessOnAllMonitorsCommand());

            // Assert
            _monitorDimmingServiceMock.Verify(x => x.UndimMonitorAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WhenMonitorsAreDimmed_UndimsEveryOne()
        {
            // Arrange
            SetupDimmedMonitors(new Dictionary<string, uint> { [HardwareId] = 80, [SecondHardwareId] = 60 });

            // Act
            await _handler.HandleAsync(new RestoreBrightnessOnAllMonitorsCommand());

            // Assert
            _monitorDimmingServiceMock.Verify(x => x.UndimMonitorAsync(HardwareId), Times.Once);
            _monitorDimmingServiceMock.Verify(x => x.UndimMonitorAsync(SecondHardwareId), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenTheServiceThrows_PropagatesTheException()
        {
            // Arrange
            SetupDimmedMonitors(new Dictionary<string, uint> { [HardwareId] = 80 });
            _monitorDimmingServiceMock
                .Setup(x => x.UndimMonitorAsync(HardwareId))
                .ThrowsAsync(new Exception("Simulated failure"));

            // Act
            var exception = await Record.ExceptionAsync(() => _handler.HandleAsync(new RestoreBrightnessOnAllMonitorsCommand()));

            // Assert
            Assert.NotNull(exception);
        }

        /// <summary>
        /// Makes GetDimmedMonitors() return the supplied recordings.
        /// </summary>
        /// <param name="recordings">The map from hardware ID to raw pre-dim brightness.</param>
        private void SetupDimmedMonitors(Dictionary<string, uint> recordings)
        {
            _monitorDimmingServiceMock.Setup(x => x.GetDimmedMonitors()).Returns(recordings);
        }
    }
}
