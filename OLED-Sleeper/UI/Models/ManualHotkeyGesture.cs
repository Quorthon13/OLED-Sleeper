using System.Windows.Input;

namespace OLED_Sleeper.UI.Models
{

    /// <summary>A keyboard shortcut stored as a readable, keyboard-layout-independent key name.</summary>
    public readonly record struct ManualHotkeyGesture(uint Modifiers, uint VirtualKey)
    {
        public static bool IsSupportedKey(Key key) => key is not (Key.None or Key.System or Key.ImeProcessed or Key.DeadCharProcessed or
            Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            && KeyInterop.VirtualKeyFromKey(key) is > 0 and <= 255;

        public static string Capture(Key key, ModifierKeys modifiers)
        {
            if (!IsSupportedKey(key) || modifiers == ModifierKeys.None) return string.Empty;
            var parts = new List<string>();
            if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
            parts.Add(key >= Key.D0 && key <= Key.D9 ? ((int)key - (int)Key.D0).ToString() : key.ToString());
            return string.Join("+", parts);
        }

        public static bool TryParse(string? text, out ManualHotkeyGesture gesture)
        {
            gesture = default;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var parts = text.Split('+', StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || parts.Any(string.IsNullOrEmpty)) return false;
            uint modifiers = 0;
            foreach (var part in parts[..^1])
            {
                uint flag = part.ToLowerInvariant() switch
                {
                    "ctrl" or "control" => 2,
                    "alt" => 1,
                    "shift" => 4,
                    "win" or "windows" => 8,
                    _ => 0
                };
                if (flag == 0 || (modifiers & flag) != 0) return false;
                modifiers |= flag;
            }
            var keyName = parts[^1];
            if (keyName.Length == 1 && keyName[0] is >= '0' and <= '9') keyName = "D" + keyName;
            if (int.TryParse(keyName, out _) || !Enum.TryParse<Key>(keyName, true, out var key) || !IsSupportedKey(key)) return false;
            gesture = new ManualHotkeyGesture(modifiers, (uint)KeyInterop.VirtualKeyFromKey(key));
            return true;
        }
    }
}
