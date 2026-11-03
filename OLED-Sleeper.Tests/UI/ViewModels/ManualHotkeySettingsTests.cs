using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.UI.ViewModels;
using System.Text.Json;

namespace OLED_Sleeper.Tests.UI.ViewModels
{

    public class ManualHotkeySettingsTests
    {
        [Fact]
        public void OldSettings_DefaultToNoShortcut()
        {
            var settings = JsonSerializer.Deserialize<MonitorSettings>("{\"HardwareId\":\"external\"}")!;
            Assert.Equal(string.Empty, settings.ManualHotkey);
        }

        [Fact]
        public void Shortcut_PersistsWithoutEnablingIdleManagement()
        {
            var settings = new MonitorSettings { HardwareId = "external", ManualHotkey = "Shift+Win+4", IsManaged = false };
            var saved = JsonSerializer.Deserialize<MonitorSettings>(JsonSerializer.Serialize(settings))!;
            Assert.Equal("Shift+Win+4", saved.ManualHotkey);
            Assert.False(saved.IsManaged);
        }

        [Fact]
        public void ShortcutChanges_SupportSaveAndDiscard()
        {
            var vm = new MonitorConfigurationViewModel(new MonitorInfo { HardwareId = "external" });
            vm.ManualHotkey = "Shift+4";
            Assert.True(vm.IsDirty);
            Assert.Equal("Shift+4", vm.ToSettings().ManualHotkey);
            vm.MarkAsSaved();
            Assert.False(vm.IsDirty);
            vm.ManualHotkey = "Ctrl+F10";
            vm.Revert();
            Assert.Equal("Shift+4", vm.ManualHotkey);
            Assert.False(vm.IsDirty);
        }
    }
}
