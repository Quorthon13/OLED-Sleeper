using OLED_Sleeper.Features.MonitorIdleDetection.Services;
using OLED_Sleeper.Features.MonitorInformation.Models;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorIdleDetection.Services
{

    public class ManualWakeTrackerTests
    {
        private static readonly Rect Bounds = new(1920, 0, 1920, 1080);
        private readonly ManualWakeTracker _tracker = new();

        [Fact]
        public void MouseEntersTargetMonitor_WakesOnce()
        {
            _tracker.Track("external", Bounds, new Point(100, 100));
            Assert.Empty(_tracker.TakeAwakenedMonitors(new Point(200, 200)));
            Assert.Equal(new[] { "external" }, _tracker.TakeAwakenedMonitors(new Point(2000, 100)));
            Assert.Empty(_tracker.TakeAwakenedMonitors(new Point(2001, 100)));
        }

        [Fact]
        public void PointerAlreadyOnTarget_RequiresMovementAfterBlackout()
        {
            _tracker.Track("external", Bounds, new Point(2000, 100));
            Assert.Empty(_tracker.TakeAwakenedMonitors(new Point(2000, 100)));
            Assert.Single(_tracker.TakeAwakenedMonitors(new Point(2001, 100)));
        }

        [Fact]
        public void Clear_CancelsMouseWake()
        {
            _tracker.Track("external", Bounds, new Point(0, 0));
            _tracker.Clear("external");
            Assert.Empty(_tracker.TakeAwakenedMonitors(new Point(2000, 100)));
        }

        [Fact]
        public void DisplayRefresh_UpdatesBoundsAndDropsDisconnectedDisplays()
        {
            _tracker.Track("external", Bounds, new Point(0, 0));
            _tracker.Track("removed", Bounds, new Point(0, 0));
            _tracker.SynchronizeBounds(new[] { new MonitorInfo { HardwareId = "external", Bounds = new Rect(-1920, 0, 1920, 1080) } });
            Assert.Empty(_tracker.TakeAwakenedMonitors(new Point(2000, 100)));
            Assert.Equal(new[] { "external" }, _tracker.TakeAwakenedMonitors(new Point(-100, 100)));
        }
    }
}
