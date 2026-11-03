using OLED_Sleeper.UI.Models;
using System.Windows.Input;

namespace OLED_Sleeper.Tests.UI.Models
{

    public class ManualHotkeyGestureTests
    {
        [Theory]
        [InlineData(Key.D4, ModifierKeys.Shift, "Shift+4", 4u, 0x34u)]
        [InlineData(Key.D4, ModifierKeys.Windows | ModifierKeys.Shift, "Shift+Win+4", 12u, 0x34u)]
        [InlineData(Key.F10, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl+Alt+F10", 3u, 0x79u)]
        [InlineData(Key.NumPad4, ModifierKeys.Control, "Ctrl+NumPad4", 2u, 0x64u)]
        public void Capture_RoundTripsPhysicalKeyAndModifiers(Key key, ModifierKeys modifiers, string expected, uint flags, uint vk)
        {
            Assert.Equal(expected, ManualHotkeyGesture.Capture(key, modifiers));
            Assert.True(ManualHotkeyGesture.TryParse(expected, out var gesture));
            Assert.Equal(new ManualHotkeyGesture(flags, vk), gesture);
        }

        [Fact]
        public void Capture_AllSupportedWpfKeysAndModifierCombinations_RoundTrip()
        {
            foreach (var key in Enum.GetValues<Key>().Distinct().Where(ManualHotkeyGesture.IsSupportedKey))
            {
                for (int flags = 1; flags <= 15; flags++)
                {
                    var capture = ManualHotkeyGesture.Capture(key, (ModifierKeys)flags);
                    Assert.True(ManualHotkeyGesture.TryParse(capture, out var gesture), capture);
                    Assert.Equal((uint)KeyInterop.VirtualKeyFromKey(key), gesture.VirtualKey);
                    Assert.Equal((uint)flags, gesture.Modifiers);
                }
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("4")]
        [InlineData("Win")]
        [InlineData("Shift+System")]
        [InlineData("Ctrl+999")]
        [InlineData("Ctrl+Ctrl+A")]
        [InlineData("Ctrl++A")]
        [InlineData("Ctrl+LWin")]
        [InlineData("NoSuchModifier+A")]
        public void TryParse_RejectsIncompleteOrInvalidShortcuts(string text) => Assert.False(ManualHotkeyGesture.TryParse(text, out _));

        [Fact]
        public void TryParse_AcceptsLegacyNamesAndCase()
        {
            Assert.True(ManualHotkeyGesture.TryParse("control+windows+d4", out var gesture));
            Assert.Equal(new ManualHotkeyGesture(10, 0x34), gesture);
        }
    }
}
