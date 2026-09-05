using Moq;
using OLED_Sleeper.Features.MonitorDimming.Services;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Tests.TestDoubles;

namespace OLED_Sleeper.Tests.Features.MonitorDimming.Services
{
    public class MonitorDimmingServiceTests
    {
        private const string HardwareId = "MON-PRIMARY";
        private const string DeviceName = @"\\.\DISPLAY1";
        private const string SecondHardwareId = "MON-SECOND";
        private const string SecondDeviceName = @"\\.\DISPLAY2";

        private readonly Mock<IMonitorInfoManager> _monitorManager;
        private readonly Mock<IOriginalBrightnessStore> _store;
        private readonly FakeDdcCiAccess _ddcCiAccess;
        private readonly FakeDdcCiSession _panel;
        private readonly FakeDdcCiSession _secondPanel;
        private readonly MonitorDimmingService _service;

        public MonitorDimmingServiceTests()
        {
            _monitorManager = new Mock<IMonitorInfoManager>();
            _store = new Mock<IOriginalBrightnessStore>();
            _ddcCiAccess = new FakeDdcCiAccess();

            SetupMonitors(
                CreateMonitor(HardwareId, DeviceName, 255),
                CreateMonitor(SecondHardwareId, SecondDeviceName, 255));

            _panel = _ddcCiAccess.AddPanel(DeviceName, 80);
            _secondPanel = _ddcCiAccess.AddPanel(SecondDeviceName, 80);

            _service = new MonitorDimmingService(_monitorManager.Object, _ddcCiAccess, _store.Object);
        }

        [Fact]
        public async Task DimMonitorAsync_WhenMonitorIsReachable_RecordsTheCurrentBrightnessBeforeWritingTheScaledValue()
        {
            // Arrange
            var recordedBeforeAnyWrite = false;
            _store.Setup(s => s.RecordOriginal(HardwareId, 80u))
                .Callback(() => recordedBeforeAnyWrite = _panel.WrittenBrightnessLevels.Count == 0);

            // Act
            await _service.DimMonitorAsync(HardwareId, 15);

            // Assert
            _store.Verify(s => s.RecordOriginal(HardwareId, 80u), Times.Once);
            Assert.True(recordedBeforeAnyWrite);
            Assert.Equal(new[] { 38u }, _panel.WrittenBrightnessLevels);
        }

        [Fact]
        public async Task DimMonitorAsync_WhenTheBrightnessReadFails_RecordsNothingAndWritesNothing()
        {
            // Arrange
            _panel.ReadsFail = true;

            // Act
            await _service.DimMonitorAsync(HardwareId, 15);

            // Assert
            _store.Verify(s => s.RecordOriginal(It.IsAny<string>(), It.IsAny<uint>()), Times.Never);
            Assert.Empty(_panel.WrittenBrightnessLevels);
        }

        [Fact]
        public async Task DimMonitorAsync_WhenMonitorReportedNoRange_WritesThePercentageUnscaled()
        {
            // Arrange
            SetupMonitors(CreateMonitor(HardwareId, DeviceName, 0));

            // Act
            await _service.DimMonitorAsync(HardwareId, 15);

            // Assert
            Assert.Equal(new[] { 15u }, _panel.WrittenBrightnessLevels);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task DimMonitorAsync_WhenHardwareIdIsMissing_OpensNoChannel(string? hardwareId)
        {
            // Act
            await _service.DimMonitorAsync(hardwareId, 15);

            // Assert
            Assert.Empty(_ddcCiAccess.OpenAttempts);
        }

        [Fact]
        public async Task DimMonitorAsync_WhenTheWriteIsRejected_KeepsTheRecordingWithoutRetrying()
        {
            // Arrange
            _panel.WritesAreRejected = true;

            // Act
            await _service.DimMonitorAsync(HardwareId, 15);

            // Assert
            _store.Verify(s => s.RecordOriginal(HardwareId, 80u), Times.Once);
            Assert.Equal(new[] { 38u }, _panel.WrittenBrightnessLevels);
            Assert.Single(_ddcCiAccess.OpenAttempts);
        }

        [Fact]
        public async Task DimMonitorAsync_WhenMonitorIsNotAttached_OpensNoChannel()
        {
            // Act
            await _service.DimMonitorAsync("MON-UNKNOWN", 15);

            // Assert
            Assert.Empty(_ddcCiAccess.OpenAttempts);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task UndimMonitorAsync_WhenHardwareIdIsMissing_OpensNoChannel(string? hardwareId)
        {
            // Act
            await _service.UndimMonitorAsync(hardwareId!);

            // Assert
            Assert.Empty(_ddcCiAccess.OpenAttempts);
        }

        [Fact]
        public async Task UndimMonitorAsync_WhenTheReadBackIsUnanswered_RetriesAndKeepsTheRecording()
        {
            // Arrange
            SetupRecording(HardwareId, 80);
            _panel.ReadsFail = true;

            // Act
            await _service.UndimMonitorAsync(HardwareId);

            // Assert
            Assert.Equal(new[] { 80u, 80u, 80u }, _panel.WrittenBrightnessLevels);
            _store.Verify(s => s.RemoveOriginal(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UndimMonitorAsync_WhenMonitorHasNoRecording_OpensNoChannel()
        {
            // Act
            await _service.UndimMonitorAsync(HardwareId);

            // Assert
            Assert.Empty(_ddcCiAccess.OpenAttempts);
        }

        [Fact]
        public async Task UndimMonitorAsync_WhenTheRestoreIsConfirmed_WritesTheRawOriginalAndClearsTheRecording()
        {
            // Arrange
            SetupRecording(HardwareId, 80);

            // Act
            await _service.UndimMonitorAsync(HardwareId);

            // Assert
            Assert.Equal(new[] { 80u }, _panel.WrittenBrightnessLevels);
            _store.Verify(s => s.RemoveOriginal(HardwareId), Times.Once);
        }

        [Fact]
        public async Task UndimMonitorAsync_WhenTheReadBackIsWithinTolerance_ConfirmsTheRestore()
        {
            // Arrange
            SetupRecording(HardwareId, 80);
            _panel.ReadBackOverride = 82;

            // Act
            await _service.UndimMonitorAsync(HardwareId);

            // Assert
            _store.Verify(s => s.RemoveOriginal(HardwareId), Times.Once);
        }

        [Fact]
        public async Task UndimMonitorAsync_WhenTheReadBackIsOutsideTolerance_RetriesAndKeepsTheRecording()
        {
            // Arrange
            SetupRecording(HardwareId, 80);
            _panel.ReadBackOverride = 83;

            // Act
            await _service.UndimMonitorAsync(HardwareId);

            // Assert
            Assert.Equal(3, _panel.WrittenBrightnessLevels.Count);
            _store.Verify(s => s.RemoveOriginal(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UndimMonitorAsync_WhenTheWriteIsRejected_RetriesThreeTimesAndKeepsTheRecording()
        {
            // Arrange
            SetupRecording(HardwareId, 80);
            _panel.WritesAreRejected = true;

            // Act
            await _service.UndimMonitorAsync(HardwareId);

            // Assert
            Assert.Equal(3, _panel.WrittenBrightnessLevels.Count);
            _store.Verify(s => s.RemoveOriginal(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UndimMonitorAsync_WhenMonitorIsUnreachable_KeepsTheRecordingWithoutRetrying()
        {
            // Arrange
            const string unreachableHardwareId = "MON-OFF";
            SetupMonitors(CreateMonitor(unreachableHardwareId, @"\\.\DISPLAY9", 255));
            SetupRecording(unreachableHardwareId, 80);

            // Act
            await _service.UndimMonitorAsync(unreachableHardwareId);

            // Assert
            Assert.Single(_ddcCiAccess.OpenAttempts);
            _store.Verify(s => s.RemoveOriginal(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UndimMonitorAsync_WhenEveryAttemptFails_DisposesEveryChannelItOpened()
        {
            // Arrange
            SetupRecording(HardwareId, 80);
            _panel.WritesAreRejected = true;

            // Act
            await _service.UndimMonitorAsync(HardwareId);

            // Assert
            Assert.Equal(3, _ddcCiAccess.OpenAttempts.Count);
            Assert.Equal(3, _panel.DisposeCount);
        }

        [Fact]
        public void GetDimmedMonitors_WhenCalled_ReturnsWhatTheStoreHolds()
        {
            // Arrange
            var recordings = new Dictionary<string, uint> { [HardwareId] = 80 };
            _store.Setup(s => s.GetAll()).Returns(recordings);

            // Act
            var dimmed = _service.GetDimmedMonitors();

            // Assert
            Assert.Equal(recordings, dimmed);
        }

        [Fact]
        public async Task DimMonitorAsync_WhenTheSameMonitorIsBusy_WaitsForTheGate()
        {
            // Arrange
            using var release = new ManualResetEventSlim();
            var reachedPanel = HoldTheFirstChannelOpen(release);
            var first = Task.Run(() => _service.DimMonitorAsync(HardwareId, 15));
            await reachedPanel.Task;

            // Act
            var second = Task.Run(() => _service.DimMonitorAsync(HardwareId, 15));
            await Task.Delay(100);

            // Assert
            Assert.Single(_ddcCiAccess.OpenAttempts);
            Assert.False(second.IsCompleted);
            release.Set();
            await first;
            await second;
        }

        [Fact]
        public async Task UndimMonitorAsync_WhenADimIsInFlightOnTheSameMonitor_WaitsForTheGate()
        {
            // Arrange
            SetupRecording(HardwareId, 80);
            using var release = new ManualResetEventSlim();
            var reachedPanel = HoldTheFirstChannelOpen(release);
            var dim = Task.Run(() => _service.DimMonitorAsync(HardwareId, 15));
            await reachedPanel.Task;

            // Act
            var undim = Task.Run(() => _service.UndimMonitorAsync(HardwareId));
            await Task.Delay(100);

            // Assert
            Assert.Single(_ddcCiAccess.OpenAttempts);
            Assert.False(undim.IsCompleted);
            release.Set();
            await dim;
            await undim;
            Assert.Equal(new[] { 38u, 80u }, _panel.WrittenBrightnessLevels);
        }

        [Fact]
        public async Task DimMonitorAsync_WhenADifferentMonitorIsBusy_RunsWithoutWaiting()
        {
            // Arrange
            using var release = new ManualResetEventSlim();
            var reachedPanel = HoldTheFirstChannelOpen(release);
            var first = Task.Run(() => _service.DimMonitorAsync(HardwareId, 15));
            await reachedPanel.Task;

            // Act
            await _service.DimMonitorAsync(SecondHardwareId, 15);

            // Assert
            Assert.Equal(new[] { 38u }, _secondPanel.WrittenBrightnessLevels);
            release.Set();
            await first;
        }

        /// <summary>
        /// Builds an attached, DDC/CI-capable monitor.
        /// </summary>
        /// <param name="hardwareId">The hardware ID the service keys everything on.</param>
        /// <param name="deviceName">The device name the channel is opened against.</param>
        /// <param name="maxBrightness">The monitor's highest accepted brightness value.</param>
        /// <returns>The monitor.</returns>
        private static MonitorInfo CreateMonitor(string hardwareId, string deviceName, uint maxBrightness)
        {
            return new MonitorInfo
            {
                HardwareId = hardwareId,
                DeviceName = deviceName,
                Capabilities = new DdcCiCapabilities(true, maxBrightness)
            };
        }

        /// <summary>
        /// Makes GetCurrentMonitorsAsync() return the supplied monitors, replacing any earlier list.
        /// </summary>
        /// <param name="monitors">The monitors to report as attached.</param>
        private void SetupMonitors(params MonitorInfo[] monitors)
        {
            _monitorManager.Setup(m => m.GetCurrentMonitorsAsync()).ReturnsAsync(monitors);
        }

        /// <summary>
        /// Makes the store report a recorded pre-dim brightness for the given monitor.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="brightness">The raw brightness value to report as recorded.</param>
        private void SetupRecording(string hardwareId, uint brightness)
        {
            var recorded = brightness;
            _store.Setup(s => s.TryGetOriginal(hardwareId, out recorded)).Returns(true);
        }

        /// <summary>
        /// Blocks inside the primary monitor's channel until the event is set, so its gate stays held
        /// while the test starts a second operation.
        /// </summary>
        /// <param name="release">The event that lets the held operation finish.</param>
        /// <returns>A task that completes once the operation is inside the channel.</returns>
        private TaskCompletionSource HoldTheFirstChannelOpen(ManualResetEventSlim release)
        {
            var reachedPanel = new TaskCompletionSource();
            _ddcCiAccess.OnOpen = deviceName =>
            {
                if (deviceName != DeviceName) return;

                reachedPanel.TrySetResult();
                release.Wait();
            };

            return reachedPanel;
        }
    }
}
