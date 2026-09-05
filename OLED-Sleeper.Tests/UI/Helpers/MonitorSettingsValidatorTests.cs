using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.ViewModels;
using System.Windows;

namespace OLED_Sleeper.Tests.UI.Helpers
{
    public class MonitorSettingsValidatorTests
    {
        [Fact]
        public void BuildValidationError_WhenEveryManagedMonitorIsValid_ReturnsNull()
        {
            // Arrange
            var monitor = CreateMonitor(1);
            monitor.Configuration.IsManaged = true;
            monitor.Configuration.Behavior = MonitorBehaviorType.Blackout;

            // Act
            var error = MonitorSettingsValidator.BuildValidationError(new[] { monitor });

            // Assert
            Assert.Null(error);
        }

        [Fact]
        public void BuildValidationError_WhenAnInvalidMonitorIsUnmanaged_ReturnsNull()
        {
            // Arrange
            var monitor = CreateMonitor(1);
            monitor.Configuration.IdleValue = 0;

            // Act
            var error = MonitorSettingsValidator.BuildValidationError(new[] { monitor });

            // Assert
            Assert.Null(error);
        }

        [Fact]
        public void BuildValidationError_WhenMonitorIsManagedAndInvalid_NamesItAndListsEveryProblem()
        {
            // Arrange
            var monitor = CreateMonitor(2);
            monitor.Configuration.IsManaged = true;
            monitor.Configuration.IdleValue = null;
            monitor.Configuration.IsActiveOnMousePosition = false;

            // Act
            var error = MonitorSettingsValidator.BuildValidationError(new[] { monitor });

            // Assert
            Assert.NotNull(error);
            Assert.Contains("Monitor 2", error);
            Assert.Contains("A monitor behavior must be selected.", error);
            Assert.Contains("Idle time value must be a number greater than zero.", error);
            Assert.Contains("At least one 'Consider Active When' option must be selected.", error);
        }

        [Fact]
        public void BuildValidationError_WithSeveralInvalidMonitors_ListsThemAll()
        {
            // Arrange
            var first = CreateMonitor(1);
            var second = CreateMonitor(2);
            var valid = CreateMonitor(3);
            first.Configuration.IsManaged = true;
            second.Configuration.IsManaged = true;
            valid.Configuration.IsManaged = true;
            valid.Configuration.Behavior = MonitorBehaviorType.Blackout;

            // Act
            var error = MonitorSettingsValidator.BuildValidationError(new[] { first, second, valid });

            // Assert
            Assert.NotNull(error);
            Assert.Contains("Monitor 1", error);
            Assert.Contains("Monitor 2", error);
            Assert.DoesNotContain("Monitor 3", error);
        }

        /// <summary>
        /// Builds a layout view model over a 1920x1080 monitor with default settings.
        /// </summary>
        private static MonitorLayoutViewModel CreateMonitor(int displayNumber)
        {
            var bounds = new Rect(0, 0, 1920, 1080);
            var monitorInfo = new MonitorInfo
            {
                HardwareId = $"MON-{displayNumber}",
                DisplayNumber = displayNumber,
                DeviceName = $@"\\.\DISPLAY{displayNumber}",
                Bounds = bounds
            };

            return new MonitorLayoutViewModel(monitorInfo, 1.0, bounds, 0, 0);
        }
    }
}
