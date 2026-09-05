using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorState.Helpers;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorState.Helpers
{
    public class DisplaySetComparerTests
    {
        private const string PanelA = "MONITOR\\GSM5C7C\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0002";
        private const string PanelB = "MONITOR\\ACR074D\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0003";

        [Fact]
        public void AreEquivalent_WhenBothReadingsMatch_ReturnsTrue()
        {
            // Arrange
            var first = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0), Monitor("\\\\.\\DISPLAY2", PanelB, 2560) };
            var second = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0), Monitor("\\\\.\\DISPLAY2", PanelB, 2560) };

            // Act
            var equivalent = DisplaySetComparer.AreEquivalent(first, second);

            // Assert
            Assert.True(equivalent);
        }

        [Fact]
        public void AreEquivalent_WhenTheReadingsAreInDifferentOrder_ReturnsTrue()
        {
            // Arrange
            var first = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0), Monitor("\\\\.\\DISPLAY2", PanelB, 2560) };
            var second = new[] { Monitor("\\\\.\\DISPLAY2", PanelB, 2560), Monitor("\\\\.\\DISPLAY1", PanelA, 0) };

            // Act
            var equivalent = DisplaySetComparer.AreEquivalent(first, second);

            // Assert
            Assert.True(equivalent);
        }

        [Fact]
        public void AreEquivalent_WhenAReadingIsNull_ReturnsFalse()
        {
            // Arrange
            var reading = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0) };

            // Act & Assert
            Assert.False(DisplaySetComparer.AreEquivalent(null, reading));
            Assert.False(DisplaySetComparer.AreEquivalent(reading, null));
        }

        [Fact]
        public void AreEquivalent_WhenAMonitorWasDisconnected_ReturnsFalse()
        {
            // Arrange
            var first = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0), Monitor("\\\\.\\DISPLAY2", PanelB, 2560) };
            var second = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0) };

            // Act
            var equivalent = DisplaySetComparer.AreEquivalent(first, second);

            // Assert
            Assert.False(equivalent);
        }

        [Fact]
        public void AreEquivalent_WhenWindowsMovedAPanelToAnotherDeviceName_ReturnsFalse()
        {
            // Arrange
            var first = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0), Monitor("\\\\.\\DISPLAY2", PanelB, 2560) };
            var second = new[] { Monitor("\\\\.\\DISPLAY2", PanelA, 0), Monitor("\\\\.\\DISPLAY1", PanelB, 2560) };

            // Act
            var equivalent = DisplaySetComparer.AreEquivalent(first, second);

            // Assert
            Assert.False(equivalent);
        }

        [Fact]
        public void AreEquivalent_WhenAnotherPanelTookOverTheDeviceName_ReturnsFalse()
        {
            // Arrange
            var first = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0) };
            var second = new[] { Monitor("\\\\.\\DISPLAY1", PanelB, 0) };

            // Act
            var equivalent = DisplaySetComparer.AreEquivalent(first, second);

            // Assert
            Assert.False(equivalent);
        }

        [Fact]
        public void AreEquivalent_WhenThePrimaryDisplayMoved_ReturnsFalse()
        {
            // Arrange
            var first = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0, isPrimary: true) };
            var second = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0, isPrimary: false) };

            // Act
            var equivalent = DisplaySetComparer.AreEquivalent(first, second);

            // Assert
            Assert.False(equivalent);
        }

        [Fact]
        public void AreEquivalent_WhenAMonitorMoved_ReturnsFalse()
        {
            // Arrange
            var first = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0) };
            var second = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, -2560) };

            // Act
            var equivalent = DisplaySetComparer.AreEquivalent(first, second);

            // Assert
            Assert.False(equivalent);
        }

        [Fact]
        public void AreEquivalent_WhenTheScalingChanged_ReturnsFalse()
        {
            // Arrange
            var first = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0, dpi: 96) };
            var second = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0, dpi: 120) };

            // Act
            var equivalent = DisplaySetComparer.AreEquivalent(first, second);

            // Assert
            Assert.False(equivalent);
        }

        [Fact]
        public void AreEquivalent_WhenAHardwareIdMomentarilyDidNotResolve_ReturnsTrue()
        {
            // Arrange
            var first = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0), Monitor("\\\\.\\DISPLAY2", PanelB, 2560) };
            var second = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0), Monitor("\\\\.\\DISPLAY2", string.Empty, 2560) };

            // Act
            var equivalent = DisplaySetComparer.AreEquivalent(first, second);

            // Assert
            Assert.True(equivalent);
        }

        [Fact]
        public void AreEquivalent_WhenAHardwareIdResolvedAgain_ReturnsTrue()
        {
            // Arrange
            var first = new[] { Monitor("\\\\.\\DISPLAY1", string.Empty, 0) };
            var second = new[] { Monitor("\\\\.\\DISPLAY1", PanelA, 0) };

            // Act
            var equivalent = DisplaySetComparer.AreEquivalent(first, second);

            // Assert
            Assert.True(equivalent);
        }

        [Fact]
        public void AreEquivalent_WhenAnUnresolvedMonitorMoved_ReturnsFalse()
        {
            // Arrange
            var first = new[] { Monitor("\\\\.\\DISPLAY1", string.Empty, 0) };
            var second = new[] { Monitor("\\\\.\\DISPLAY1", string.Empty, -2560) };

            // Act
            var equivalent = DisplaySetComparer.AreEquivalent(first, second);

            // Assert
            Assert.False(equivalent);
        }

        private static MonitorInfo Monitor(
            string deviceName,
            string hardwareId,
            double left,
            bool isPrimary = false,
            uint dpi = 96)
        {
            return new MonitorInfo
            {
                DeviceName = deviceName,
                HardwareId = hardwareId,
                Bounds = new Rect(left, 0, 2560, 1440),
                IsPrimary = isPrimary,
                Dpi = dpi,
                DisplayNumber = int.Parse(deviceName[^1..])
            };
        }
    }
}
