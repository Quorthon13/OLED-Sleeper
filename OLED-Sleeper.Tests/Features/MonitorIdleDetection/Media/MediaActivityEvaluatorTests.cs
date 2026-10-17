using OLED_Sleeper.Features.MonitorIdleDetection.Media.Helpers;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Models;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorIdleDetection.Media
{
    public class MediaActivityEvaluatorTests
    {
        private const string Browser = @"c:\apps\browser.exe";
        private const string Music = @"c:\apps\music.exe";

        private static readonly Rect Oled = new(0, 0, 1920, 1080);
        private static readonly Rect Other = new(1920, 0, 1920, 1080);
        private static readonly Rect Windowed = new(100, 100, 1200, 800);

        [Theory]
        [InlineData(true, false, "fullscreen+display-required")]
        [InlineData(false, true, "fullscreen+audio(browser.exe)")]
        [InlineData(false, false, "")]
        public void Evaluate_ForAFullscreenWindow_FollowsDisplayRequiredAndAudioOfThatWindow(bool displayRequired, bool windowIsAudible, string expectedSignal)
        {
            // Act
            var decision = Evaluate(displayRequired, audible: windowIsAudible ? new[] { Browser } : null, windows: new[] { Fullscreen(Browser, Oled) });

            // Assert
            Assert.Equal(expectedSignal != "", decision.IsVideoPlaying);
            Assert.Equal(expectedSignal, decision.Signal);
        }

        [Fact]
        public void Evaluate_ForAFullscreenWindowWhoseAudioComesFromAnotherFamily_IsNotVideo()
        {
            Assert.False(Evaluate(false, audible: new[] { Music }, windows: new[] { Fullscreen(Browser, Oled) }).IsVideoPlaying);
        }

        [Fact]
        public void Evaluate_ForAFullscreenWindowOnTheOtherMonitor_IsNotVideoHere()
        {
            Assert.False(Evaluate(true, audible: new[] { Browser }, windows: new[] { Fullscreen(Browser, Other) }).IsVideoPlaying);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(5, false)]
        public void Evaluate_ForAFullscreenWindowShortOfTheMonitorEdges_AllowsTwoPixelsOfTolerance(int inset, bool expected)
        {
            // Arrange
            var bounds = new Rect(inset, inset, 1920 - 2 * inset, 1080 - 2 * inset);

            // Act
            var decision = Evaluate(true, windows: new[] { Fullscreen(Browser, bounds) });

            // Assert
            Assert.Equal(expected, decision.IsVideoPlaying);
        }

        [Fact]
        public void Evaluate_WhenAnotherWindowIsAboveTheFullscreenOne_UsesTheTopmostWindowOnTheMonitor()
        {
            Assert.False(Evaluate(true, windows: new[] { Captioned(Music, new Rect(50, 50, 400, 300)), Fullscreen(Browser, Oled) }).IsVideoPlaying);
        }

        [Fact]
        public void Evaluate_WhenTheTopWindowIsOnTheOtherMonitor_LooksAtTheTopWindowOnThisOne()
        {
            Assert.True(Evaluate(true, windows: new[] { Captioned(Music, new Rect(2000, 50, 400, 300)), Fullscreen(Browser, Oled) }).IsVideoPlaying);
        }

        [Fact]
        public void Evaluate_ForAMaximizedWindowThatCoversTheMonitorBecauseTheTaskbarAutoHides_IsNotFullscreen()
        {
            // Display-required is set by a video on the other monitor, and this monitor is static.
            Assert.False(Evaluate(true, windows: new[] { Captioned(Browser, Oled) }).IsVideoPlaying);
            Assert.False(Evaluate(false, audible: new[] { Browser }, windows: new[] { Captioned(Browser, Oled) }).IsVideoPlaying);
        }

        [Fact]
        public void Evaluate_ForAMaximizedWindowShowingMotionWithDisplayRequired_IsVideo()
        {
            Assert.Equal("motion+display-required", Evaluate(true, isMoving: true, windows: new[] { Captioned(Browser, Oled) }).Signal);
        }

        [Fact]
        public void Evaluate_ForMotionWithDisplayRequired_IsVideoInAWindow()
        {
            Assert.Equal("motion+display-required", Evaluate(true, isMoving: true, windows: new[] { Captioned(Browser, Windowed) }).Signal);
        }

        [Fact]
        public void Evaluate_ForMotionWithAudioFromAWindowOnTheMonitor_IsVideo()
        {
            Assert.Equal("motion+audio(browser.exe)", Evaluate(false, audible: new[] { Browser }, isMoving: true, windows: new[] { Captioned(Browser, Windowed) }).Signal);
        }

        [Fact]
        public void Evaluate_ForAudioWithoutMotion_IsNotVideo()
        {
            Assert.False(Evaluate(false, audible: new[] { Music }, windows: new[] { Captioned(Music, Windowed) }).IsVideoPlaying);
        }

        [Fact]
        public void Evaluate_ForDisplayRequiredOnAStaticMonitor_IsNotVideo()
        {
            Assert.False(Evaluate(true, windows: new[] { Captioned(Browser, Windowed) }).IsVideoPlaying);
        }

        [Fact]
        public void Evaluate_ForMotionWithoutDisplayRequiredOrAudio_IsNotVideo()
        {
            Assert.False(Evaluate(false, isMoving: true, windows: new[] { Captioned(Browser, Windowed) }).IsVideoPlaying);
        }

        [Fact]
        public void Evaluate_ForMotionWithAudioFromAWindowOnlyOnTheOtherMonitor_IsNotVideoHere()
        {
            var windows = new[] { Captioned(Music, Other), Captioned(Browser, Windowed) };
            Assert.False(Evaluate(false, audible: new[] { Music }, isMoving: true, windows: windows).IsVideoPlaying);
        }

        private static MediaDecision Evaluate(bool displayRequired, string[]? audible = null, bool isMoving = false, WindowSnapshot[]? windows = null)
        {
            return MediaActivityEvaluator.Evaluate(
                displayRequired,
                new HashSet<string>(audible ?? Array.Empty<string>(), StringComparer.Ordinal),
                windows ?? Array.Empty<WindowSnapshot>(),
                Oled,
                isMoving);
        }

        /// <summary>
        /// A window with no title bar, as a fullscreen window is.
        /// </summary>
        private static WindowSnapshot Fullscreen(string family, Rect bounds) => new(family, bounds, HasCaption: false);

        /// <summary>
        /// A window with a title bar, as an ordinary or maximized window is.
        /// </summary>
        private static WindowSnapshot Captioned(string family, Rect bounds) => new(family, bounds, HasCaption: true);
    }
}
