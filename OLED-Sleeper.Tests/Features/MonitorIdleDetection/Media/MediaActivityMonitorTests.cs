using Moq;
using OLED_Sleeper.Features.MonitorIdleDetection.Media;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Models;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorIdleDetection.Media
{
    public class MediaActivityMonitorTests
    {
        private const string Browser = @"c:\apps\browser.exe";
        private static readonly Rect Oled = new(0, 0, 1920, 1080);

        private readonly Mock<IDisplayRequiredSignal> _displayRequired = new();
        private readonly Mock<IAudioActivityProbe> _audio = new();
        private readonly Mock<IWindowSnapshotProvider> _windows = new();
        private readonly Mock<IScreenMotionDetector> _motion = new();
        private readonly MediaActivityMonitor _monitor;

        public MediaActivityMonitorTests()
        {
            _displayRequired.Setup(s => s.IsDisplayRequired()).Returns(true);
            _audio.Setup(a => a.GetAudibleFamilies()).Returns(new HashSet<string>());
            _windows.Setup(w => w.GetSnapshot()).Returns(Snapshot(Fullscreen(Browser)));
            _monitor = new MediaActivityMonitor(_displayRequired.Object, _audio.Object, _windows.Object, _motion.Object);
        }

        [Fact]
        public void Evaluate_WithNoRequests_ProbesAndCapturesNothing()
        {
            // Act
            var decisions = _monitor.Evaluate(Array.Empty<MediaMonitorRequest>());

            // Assert
            Assert.Empty(decisions);
            _displayRequired.Verify(s => s.IsDisplayRequired(), Times.Never);
            _audio.Verify(a => a.GetAudibleFamilies(), Times.Never);
            _windows.Verify(w => w.GetSnapshot(), Times.Never);
            _motion.Verify(m => m.IsMoving(It.IsAny<string>(), It.IsAny<Rect>()), Times.Never);
        }

        [Fact]
        public void Evaluate_ForAFullscreenWindowWithDisplayRequired_DecidesVideoIsPlaying()
        {
            // Act
            var decisions = _monitor.Evaluate(new[] { Request("A") });

            // Assert
            Assert.True(decisions["A"].IsVideoPlaying);
            _motion.Verify(m => m.IsMoving("A", Oled), Times.Once);
        }

        [Fact]
        public void Evaluate_WhileABlackoutOverlayCoversTheMonitor_DecidesNoVideoAndDoesNotCapture()
        {
            // Arrange
            _windows.Setup(w => w.GetSnapshot()).Returns(new WindowSnapshotSet(new[] { Fullscreen(Browser) }, new[] { Oled }));

            // Act
            var decisions = _monitor.Evaluate(new[] { Request("A") });

            // Assert
            Assert.False(decisions["A"].IsVideoPlaying);
            _motion.Verify(m => m.IsMoving(It.IsAny<string>(), It.IsAny<Rect>()), Times.Never);
        }

        [Fact]
        public void Evaluate_WhenAMonitorStopsBeingRequested_ForgetsItsMotionHistory()
        {
            // Arrange
            _monitor.Evaluate(new[] { Request("A") });

            // Act
            _monitor.Evaluate(Array.Empty<MediaMonitorRequest>());

            // Assert
            _motion.Verify(m => m.Forget("A"), Times.Once);
        }

        private static MediaMonitorRequest Request(string hardwareId) => new(hardwareId, Oled);

        /// <summary>
        /// A window with no title bar that covers the monitor.
        /// </summary>
        private static WindowSnapshot Fullscreen(string family) => new(family, Oled, HasCaption: false);

        private static WindowSnapshotSet Snapshot(params WindowSnapshot[] windows) => new(windows, Array.Empty<Rect>());
    }
}
