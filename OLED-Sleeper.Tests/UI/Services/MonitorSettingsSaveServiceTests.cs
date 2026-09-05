using Moq;
using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using OLED_Sleeper.UI.Models;
using OLED_Sleeper.UI.Services;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using System.Windows;

namespace OLED_Sleeper.Tests.UI.Services
{
    public class MonitorSettingsSaveServiceTests
    {
        private readonly Mock<IMonitorSettingsFileService> _settingsServiceMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly MonitorSettingsSaveService _service;

        /// <summary>The settings handed to the settings file service, or null when it was never called.</summary>
        private List<MonitorSettings>? _writtenSettings;

        public MonitorSettingsSaveServiceTests()
        {
            _settingsServiceMock = new Mock<IMonitorSettingsFileService>();
            _dialogServiceMock = new Mock<IDialogService>();

            _settingsServiceMock
                .Setup(x => x.SaveSettings(It.IsAny<List<MonitorSettings>>()))
                .Callback<List<MonitorSettings>>(settings => _writtenSettings = settings);

            _service = new MonitorSettingsSaveService(
                _settingsServiceMock.Object,
                _dialogServiceMock.Object);
        }

        [Fact]
        public void TrySave_WhenSettingsAreValid_WritesThemAndReportsSuccess()
        {
            // Arrange
            var monitor = CreateManagedMonitor("MON-1");
            monitor.Configuration.DimLevel = 50;

            // Act
            var saved = _service.TrySave(new[] { monitor });

            // Assert
            Assert.True(saved);
            Assert.NotNull(_writtenSettings);
            var entry = Assert.Single(_writtenSettings!);
            Assert.Equal("MON-1", entry.HardwareId);
            Assert.Equal(50, entry.DimLevel);
        }

        [Fact]
        public void TrySave_WhenSettingsAreValid_MarksEveryMonitorAsSaved()
        {
            // Arrange
            var first = CreateManagedMonitor("MON-1");
            var second = CreateManagedMonitor("MON-2", 2);
            first.Configuration.DimLevel = 50;
            Assert.True(first.Configuration.IsDirty);

            // Act
            _service.TrySave(new[] { first, second });

            // Assert
            Assert.False(first.Configuration.IsDirty);
            Assert.False(second.Configuration.IsDirty);
        }

        [Fact]
        public void TrySave_WhenAManagedMonitorIsInvalid_ShowsTheErrorAndWritesNothing()
        {
            // Arrange
            var monitor = CreateManagedMonitor("MON-1");
            monitor.Configuration.IdleValue = 0;

            // Act
            var saved = _service.TrySave(new[] { monitor });

            // Assert
            Assert.False(saved);
            _dialogServiceMock.Verify(x => x.ShowError(It.IsAny<string>(), "Monitor Configuration Error"), Times.Once);
            _settingsServiceMock.Verify(x => x.SaveSettings(It.IsAny<List<MonitorSettings>>()), Times.Never);
        }

        [Fact]
        public void TrySave_WhenAMonitorIsInvalidButUnmanaged_SavesAnyway()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            monitor.Configuration.IdleValue = 0;

            // Act
            var saved = _service.TrySave(new[] { monitor });

            // Assert
            Assert.True(saved);
            _dialogServiceMock.Verify(x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _settingsServiceMock.Verify(x => x.SaveSettings(It.IsAny<List<MonitorSettings>>()), Times.Once);
        }

        [Fact]
        public void TrySave_WithNoMonitors_WritesAnEmptyList()
        {
            // Act
            var saved = _service.TrySave(Array.Empty<MonitorLayoutViewModel>());

            // Assert
            Assert.True(saved);
            Assert.NotNull(_writtenSettings);
            Assert.Empty(_writtenSettings!);
        }

        /// <summary>
        /// Builds a managed monitor whose configuration is otherwise valid.
        /// </summary>
        /// <param name="hardwareId">The hardware ID for the monitor.</param>
        /// <param name="displayNumber">The display number for the monitor.</param>
        /// <returns>A layout view model over the described monitor.</returns>
        private static MonitorLayoutViewModel CreateManagedMonitor(string hardwareId, int displayNumber = 1)
        {
            var monitor = CreateMonitor(hardwareId, displayNumber);
            monitor.Configuration.ApplySettings(new MonitorSettings
            {
                HardwareId = hardwareId,
                IsManaged = true,
                Behavior = MonitorBehaviorType.Blackout,
                IdleValue = 5,
                IdleUnit = TimeUnit.Minutes,
                IsActiveOnInput = true
            });
            monitor.Configuration.MarkAsSaved();

            return monitor;
        }

        /// <summary>
        /// Builds a layout view model over a 1920x1080 monitor with the supplied hardware ID.
        /// </summary>
        /// <param name="hardwareId">The hardware ID for the monitor.</param>
        /// <param name="displayNumber">The display number for the monitor.</param>
        /// <returns>A layout view model over the described monitor.</returns>
        private static MonitorLayoutViewModel CreateMonitor(string hardwareId, int displayNumber = 1)
        {
            var bounds = new Rect(0, 0, 1920, 1080);
            var monitorInfo = new MonitorInfo
            {
                HardwareId = hardwareId,
                DisplayNumber = displayNumber,
                DeviceName = $@"\\.\DISPLAY{displayNumber}",
                Bounds = bounds
            };

            return new MonitorLayoutViewModel(monitorInfo, 1.0, bounds, 0, 0);
        }
    }
}
