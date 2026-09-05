using OLED_Sleeper.Features.MonitorBlackout.Services;
using OLED_Sleeper.Tests.TestDoubles;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorBlackout.Services
{
    public class MonitorBlackoutServiceTests
    {
        private static readonly Rect PrimaryBounds = new(0, 0, 1920, 1080);
        private static readonly Rect SecondaryBounds = new(1920, 0, 2560, 1440);

        private readonly ImmediateDispatcher _dispatcher;
        private readonly FakeOverlayWindowFactory _overlayWindowFactory;
        private readonly MonitorBlackoutService _service;

        public MonitorBlackoutServiceTests()
        {
            _dispatcher = new ImmediateDispatcher();
            _overlayWindowFactory = new FakeOverlayWindowFactory();

            _service = new MonitorBlackoutService(_dispatcher, _overlayWindowFactory);
        }

        [Fact]
        public async Task ShowBlackoutOverlayAsync_WhenMonitorHasNoOverlay_ShowsOverlayAtTheGivenBounds()
        {
            // Arrange
            var hardwareId = "MON-123";

            // Act
            await _service.ShowBlackoutOverlayAsync(hardwareId, PrimaryBounds);

            // Assert
            var overlay = Assert.Single(_overlayWindowFactory.Created);
            Assert.Equal(PrimaryBounds, overlay.ShownBounds);
            Assert.False(overlay.IsClosed);
        }

        [Fact]
        public async Task ShowBlackoutOverlayAsync_WhenCalledFromABackgroundThread_MarshalsOntoTheDispatcher()
        {
            // Arrange
            _dispatcher.IsOnUiThread = false;

            // Act
            await _service.ShowBlackoutOverlayAsync("MON-123", PrimaryBounds);

            // Assert
            Assert.Equal(1, _dispatcher.InvokeAsyncCount);
        }

        [Fact]
        public async Task ShowBlackoutOverlayAsync_WhenOverlayIsShown_TracksItsHandle()
        {
            // Arrange
            var hardwareId = "MON-123";

            // Act
            await _service.ShowBlackoutOverlayAsync(hardwareId, PrimaryBounds);

            // Assert
            var overlay = Assert.Single(_overlayWindowFactory.Created);
            Assert.True(_service.IsOverlayWindow(overlay.Handle));
        }

        [Fact]
        public async Task ShowBlackoutOverlayAsync_WhenMonitorAlreadyHasAnOverlay_DoesNotCreateASecondOne()
        {
            // Arrange
            var hardwareId = "MON-123";
            await _service.ShowBlackoutOverlayAsync(hardwareId, PrimaryBounds);

            // Act
            await _service.ShowBlackoutOverlayAsync(hardwareId, SecondaryBounds);

            // Assert
            var overlay = Assert.Single(_overlayWindowFactory.Created);
            Assert.Equal(PrimaryBounds, overlay.ShownBounds);
        }

        [Fact]
        public async Task ShowBlackoutOverlayAsync_WhenMonitorsDiffer_TracksAnOverlayPerMonitor()
        {
            // Arrange
            var firstHardwareId = "MON-123";
            var secondHardwareId = "MON-456";

            // Act
            await _service.ShowBlackoutOverlayAsync(firstHardwareId, PrimaryBounds);
            await _service.ShowBlackoutOverlayAsync(secondHardwareId, SecondaryBounds);

            // Assert
            Assert.Equal(2, _overlayWindowFactory.Created.Count);
            Assert.True(_service.IsOverlayWindow(_overlayWindowFactory.Created[0].Handle));
            Assert.True(_service.IsOverlayWindow(_overlayWindowFactory.Created[1].Handle));
        }

        [Fact]
        public async Task ShowBlackoutOverlayAsync_WhenOverlayReportsNoHandle_StillCoversTheMonitorOnce()
        {
            // Arrange
            var handlelessFactory = new FakeOverlayWindowFactory(nint.Zero);
            var service = new MonitorBlackoutService(_dispatcher, handlelessFactory);
            var hardwareId = "MON-123";

            // Act
            await service.ShowBlackoutOverlayAsync(hardwareId, PrimaryBounds);
            await service.ShowBlackoutOverlayAsync(hardwareId, PrimaryBounds);

            // Assert
            Assert.Single(handlelessFactory.Created);
            Assert.False(service.IsOverlayWindow(nint.Zero));
        }

        [Fact]
        public async Task HideBlackoutOverlayAsync_WhenMonitorHasAnOverlay_ClosesIt()
        {
            // Arrange
            var hardwareId = "MON-123";
            await _service.ShowBlackoutOverlayAsync(hardwareId, PrimaryBounds);

            // Act
            await _service.HideBlackoutOverlayAsync(hardwareId);

            // Assert
            var overlay = Assert.Single(_overlayWindowFactory.Created);
            Assert.True(overlay.IsClosed);
        }

        [Fact]
        public async Task HideBlackoutOverlayAsync_WhenCalledFromABackgroundThread_MarshalsOntoTheDispatcher()
        {
            // Arrange
            var hardwareId = "MON-123";
            await _service.ShowBlackoutOverlayAsync(hardwareId, PrimaryBounds);
            _dispatcher.IsOnUiThread = false;

            // Act
            await _service.HideBlackoutOverlayAsync(hardwareId);

            // Assert
            Assert.Equal(2, _dispatcher.InvokeAsyncCount);
        }

        [Fact]
        public async Task HideBlackoutOverlayAsync_WhenOverlayHandleWentToZeroOnClose_UntracksTheHandleRecordedWhenShowing()
        {
            // Arrange
            var hardwareId = "MON-123";
            await _service.ShowBlackoutOverlayAsync(hardwareId, PrimaryBounds);

            var overlay = Assert.Single(_overlayWindowFactory.Created);
            nint handleWhileShown = overlay.Handle;

            // Act
            await _service.HideBlackoutOverlayAsync(hardwareId);

            // Assert
            Assert.Equal(nint.Zero, overlay.Handle);
            Assert.False(_service.IsOverlayWindow(handleWhileShown));
        }

        [Fact]
        public async Task HideBlackoutOverlayAsync_WhenOtherMonitorsAreBlackedOut_LeavesTheirOverlaysAlone()
        {
            // Arrange
            var hiddenHardwareId = "MON-123";
            var keptHardwareId = "MON-456";

            await _service.ShowBlackoutOverlayAsync(hiddenHardwareId, PrimaryBounds);
            await _service.ShowBlackoutOverlayAsync(keptHardwareId, SecondaryBounds);

            var keptOverlay = _overlayWindowFactory.Created[1];
            nint keptHandle = keptOverlay.Handle;

            // Act
            await _service.HideBlackoutOverlayAsync(hiddenHardwareId);

            // Assert
            Assert.False(keptOverlay.IsClosed);
            Assert.True(_service.IsOverlayWindow(keptHandle));
        }

        [Fact]
        public async Task HideBlackoutOverlayAsync_WhenMonitorHasNoOverlay_DoesNothing()
        {
            // Arrange
            var hardwareId = "MON-UNKNOWN";

            // Act
            var exception = await Record.ExceptionAsync(() => _service.HideBlackoutOverlayAsync(hardwareId));

            // Assert
            Assert.Null(exception);
            Assert.Empty(_overlayWindowFactory.Created);
        }

        [Fact]
        public async Task HideBlackoutOverlayAsync_WhenCalledTwiceForTheSameMonitor_ClosesTheOverlayOnce()
        {
            // Arrange
            var hardwareId = "MON-123";
            await _service.ShowBlackoutOverlayAsync(hardwareId, PrimaryBounds);
            await _service.HideBlackoutOverlayAsync(hardwareId);

            // Act
            var exception = await Record.ExceptionAsync(() => _service.HideBlackoutOverlayAsync(hardwareId));

            // Assert
            Assert.Null(exception);
            Assert.Single(_overlayWindowFactory.Created);
        }

        [Fact]
        public async Task ShowBlackoutOverlayAsync_WhenTheMonitorWasPreviouslyHidden_CreatesAFreshOverlay()
        {
            // Arrange
            var hardwareId = "MON-123";
            await _service.ShowBlackoutOverlayAsync(hardwareId, PrimaryBounds);
            await _service.HideBlackoutOverlayAsync(hardwareId);

            // Act
            await _service.ShowBlackoutOverlayAsync(hardwareId, SecondaryBounds);

            // Assert
            Assert.Equal(2, _overlayWindowFactory.Created.Count);

            var newOverlay = _overlayWindowFactory.Created[1];
            Assert.Equal(SecondaryBounds, newOverlay.ShownBounds);
            Assert.True(_service.IsOverlayWindow(newOverlay.Handle));
        }

        [Fact]
        public void IsOverlayWindow_WhenHandleIsZero_ReturnsFalse()
        {
            // Arrange, Act
            bool isOverlay = _service.IsOverlayWindow(nint.Zero);

            // Assert
            Assert.False(isOverlay);
        }

        [Fact]
        public async Task IsOverlayWindow_WhenHandleBelongsToAnotherWindow_ReturnsFalse()
        {
            // Arrange
            await _service.ShowBlackoutOverlayAsync("MON-123", PrimaryBounds);

            var overlay = Assert.Single(_overlayWindowFactory.Created);
            nint unrelatedHandle = overlay.Handle + 1;

            // Act
            bool isOverlay = _service.IsOverlayWindow(unrelatedHandle);

            // Assert
            Assert.False(isOverlay);
        }
    }
}
