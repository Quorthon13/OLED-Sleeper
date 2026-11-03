using Moq;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Messaging.Interfaces;
using OLED_Sleeper.UI.Models;
using OLED_Sleeper.UI.Services;
using OLED_Sleeper.UI.Services.Interfaces;
using System.Windows;

namespace OLED_Sleeper.Tests.UI.Services
{

    public class ManualHotkeyServiceTests
    {
        private readonly Mock<IMediator> _mediator = new();
        private readonly Mock<IMonitorBlackoutService> _blackout = new();
        private readonly Mock<IMonitorIdleDetectionService> _idle = new();
        private readonly Mock<IMonitorInfoManager> _monitors = new();
        private readonly Mock<IGlobalHotkeyRegistrar> _registrar = new();
        private readonly ManualHotkeyService _service;
        private bool _shown;
        private static readonly Rect Bounds = new(1920, 0, 1920, 1080);

        public ManualHotkeyServiceTests()
        {
            int error = 0;
            _registrar.SetupGet(r => r.IsReady).Returns(true);
            _registrar.SetupGet(r => r.CursorPosition).Returns(new Point(10, 20));
            _registrar.Setup(r => r.TryRegister(It.IsAny<int>(), It.IsAny<ManualHotkeyGesture>(), out error)).Returns(true);
            _monitors.Setup(m => m.GetCurrentMonitorsAsync()).ReturnsAsync(new[] { new MonitorInfo { HardwareId = "external", Bounds = Bounds } });
            _blackout.Setup(b => b.IsBlackoutShown("external")).Returns(() => _shown);
            _mediator.Setup(m => m.SendAsync(It.IsAny<ApplyBlackoutOverlayCommand>())).Callback(() => _shown = true).Returns(Task.CompletedTask);
            _mediator.Setup(m => m.SendAsync(It.IsAny<RestoreMonitorStateCommand>())).Callback(() => _shown = false).Returns(Task.CompletedTask);
            _service = new ManualHotkeyService(_mediator.Object, _blackout.Object, _idle.Object, _monitors.Object, _registrar.Object);
        }

        private static MonitorSettings Setting(string id = "external", string shortcut = "Shift+4") => new() { HardwareId = id, ManualHotkey = shortcut };

        [Fact]
        public void Update_RegistersShortcutForAnUnmanagedMonitor()
        {
            _service.Update(new[] { Setting() });
            _registrar.Verify(r => r.TryRegister(0x5101, new ManualHotkeyGesture(4, 0x34), out It.Ref<int>.IsAny), Times.Once);
            Assert.Empty(_service.GetShortcutError("external"));
        }

        [Fact]
        public void RegistrationFailure_IsVisibleAndDoesNotTriggerMonitor()
        {
            int error = 1409;
            _registrar.Setup(r => r.TryRegister(It.IsAny<int>(), It.IsAny<ManualHotkeyGesture>(), out error)).Returns(false);
            _service.Update(new[] { Setting() });
            Assert.Contains("unavailable", _service.GetShortcutError("external"));
            _registrar.Raise(r => r.HotkeyPressed += null, 0x5101);
            _mediator.Verify(m => m.SendAsync(It.IsAny<ApplyBlackoutOverlayCommand>()), Times.Never);
        }

        [Fact]
        public void DuplicateShortcuts_AreDisabledForBothMonitors()
        {
            _service.Update(new[] { Setting(), Setting("laptop", "shift+D4") });
            Assert.Contains("more than one", _service.GetShortcutError("external"));
            Assert.Contains("more than one", _service.GetShortcutError("laptop"));
            _registrar.Verify(r => r.Unregister(0x5101), Times.Once);
        }

        [Fact]
        public void UpdatingOrRemovingShortcut_UnregistersPreviousBindings()
        {
            _service.Update(new[] { Setting() });
            _service.Update(new[] { Setting(shortcut: "Win+F10") });
            _registrar.Verify(r => r.Unregister(0x5101), Times.Once);
            _service.Update(Array.Empty<MonitorSettings>());
            _registrar.Verify(r => r.Unregister(0x5101), Times.Exactly(2));
        }

        [Fact]
        public void CaptureFocus_SuspendsAndThenRestoresGlobalBindings()
        {
            _service.Update(new[] { Setting() });
            _service.SetCaptureActive(true);
            _registrar.Verify(r => r.Unregister(0x5101), Times.Once);
            _registrar.Raise(r => r.HotkeyPressed += null, 0x5101);
            _mediator.Verify(m => m.SendAsync(It.IsAny<ApplyBlackoutOverlayCommand>()), Times.Never);
            _service.SetCaptureActive(false);
            _registrar.Verify(r => r.TryRegister(0x5101, It.IsAny<ManualHotkeyGesture>(), out It.Ref<int>.IsAny), Times.Exactly(2));
        }

        [Fact]
        public async Task Toggle_UsesNormalBlackoutAndArmsMouseWakeWithoutEnablingIdleManagement()
        {
            _service.Update(new[] { Setting() });
            await _service.ToggleAsync("external");
            _mediator.Verify(m => m.SendAsync(It.Is<ApplyBlackoutOverlayCommand>(c => c.HardwareId == "external" && !c.LowerBrightness)), Times.Once);
            _idle.Verify(i => i.TrackManualWake("external", Bounds, new Point(10, 20)), Times.Once);
        }

        [Fact]
        public async Task Toggle_RespectsExplicitHardwareBrightnessPreference()
        {
            var setting = Setting();
            setting.LowerBrightnessOnBlackout = true;
            _service.Update(new[] { setting });
            await _service.ToggleAsync("external");
            _mediator.Verify(m => m.SendAsync(It.Is<ApplyBlackoutOverlayCommand>(c => c.LowerBrightness)), Times.Once);
        }

        [Fact]
        public async Task Toggle_WhenOverlayAlreadyExists_RestoresOverlayAndBrightnessThroughNormalCommand()
        {
            _shown = true;
            await _service.ToggleAsync("external");
            _mediator.Verify(m => m.SendAsync(It.Is<RestoreMonitorStateCommand>(c => c.HardwareId == "external")), Times.Once);
            _idle.Verify(i => i.ClearManualWake("external"), Times.Once);
        }

        [Fact]
        public async Task Toggle_WhenMonitorWasDisconnected_DoesNothing()
        {
            await _service.ToggleAsync("disconnected");
            _mediator.Verify(m => m.SendAsync(It.IsAny<ApplyBlackoutOverlayCommand>()), Times.Never);
            _idle.Verify(i => i.TrackManualWake(It.IsAny<string>(), It.IsAny<Rect>(), It.IsAny<Point>()), Times.Never);
        }

        [Fact]
        public void AutomaticWake_NotifiesTheButtonAndClearsManualWakeTracking()
        {
            string? changed = null;
            _service.BlackoutStateChanged += id => changed = id;
            _blackout.Raise(b => b.BlackoutStateChanged += null, "external");
            Assert.Equal("external", changed);
            _idle.Verify(i => i.ClearManualWake("external"), Times.Once);
        }

        [Fact]
        public async Task RapidToggles_AreSerializedUntilThePreviousBlackoutCompletes()
        {
            var applied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _mediator.Setup(m => m.SendAsync(It.IsAny<ApplyBlackoutOverlayCommand>()))
                .Callback(() => _shown = true).Returns(applied.Task);
            var first = _service.ToggleAsync("external");
            var second = _service.ToggleAsync("external");
            _mediator.Verify(m => m.SendAsync(It.IsAny<RestoreMonitorStateCommand>()), Times.Never);
            applied.SetResult();
            await Task.WhenAll(first, second);
            _mediator.Verify(m => m.SendAsync(It.IsAny<RestoreMonitorStateCommand>()), Times.Once);
        }

        [Fact]
        public void Dispose_UnregistersBindingsAndIgnoresSubsequentUpdates()
        {
            _service.Update(new[] { Setting() });
            _service.Dispose();
            _service.Dispose();
            _service.Update(new[] { Setting() });
            _registrar.Verify(r => r.Unregister(0x5101), Times.Once);
            _registrar.Verify(r => r.Dispose(), Times.Once);
        }
    }
}
