using OLED_Sleeper.Features.MonitorInformation.Models;

namespace OLED_Sleeper.Features.MonitorState.Helpers
{
    /// <summary>
    /// Decides whether two readings of the display set describe the same arrangement.
    /// Capabilities take no part in the comparison.
    /// </summary>
    public static class DisplaySetComparer
    {
        /// <summary>
        /// Compares two readings panel by panel.
        /// <para>
        /// Panels are paired by hardware ID, so a reading where Windows has moved a panel to a different
        /// device name still lines that panel up with itself and reports the move as a change. A panel whose
        /// hardware ID resolved in only one of the two readings is paired by device name instead, so a
        /// momentary resolution failure does not read as a display being unplugged.
        /// </para>
        /// </summary>
        /// <param name="first">The first reading.</param>
        /// <param name="second">The second reading.</param>
        /// <returns>True when both readings describe the same panels in the same places; otherwise, false.
        /// A null reading never compares equal.</returns>
        public static bool AreEquivalent(IReadOnlyList<MonitorInfo>? first, IReadOnlyList<MonitorInfo>? second)
        {
            if (first == null || second == null) return false;
            if (first.Count != second.Count) return false;

            var unpaired = new List<MonitorInfo>(second);
            var deferred = new List<MonitorInfo>();

            foreach (var monitor in first)
            {
                var index = FindByHardwareId(unpaired, monitor);
                if (index < 0)
                {
                    deferred.Add(monitor);
                    continue;
                }

                if (!HasSamePlacement(monitor, unpaired[index])) return false;
                unpaired.RemoveAt(index);
            }

            foreach (var monitor in deferred)
            {
                var index = FindByDeviceName(unpaired, monitor);
                if (index < 0) return false;

                if (!HasSamePlacement(monitor, unpaired[index])) return false;
                unpaired.RemoveAt(index);
            }

            return unpaired.Count == 0;
        }

        /// <summary>
        /// Finds the reading of the same panel, matching on hardware ID.
        /// </summary>
        /// <param name="candidates">The entries not yet paired.</param>
        /// <param name="monitor">The entry to pair.</param>
        /// <returns>The index of the match, or -1 when there is none.</returns>
        private static int FindByHardwareId(List<MonitorInfo> candidates, MonitorInfo monitor)
        {
            if (monitor.HardwareId.Length == 0) return -1;

            return candidates.FindIndex(candidate => candidate.HardwareId == monitor.HardwareId);
        }

        /// <summary>
        /// Finds the reading of the same panel by device name, for a panel whose hardware ID resolved in
        /// only one of the two readings. Two entries that both resolved to different hardware IDs are never
        /// paired this way — under one device name those are two different panels.
        /// </summary>
        /// <param name="candidates">The entries not yet paired.</param>
        /// <param name="monitor">The entry to pair.</param>
        /// <returns>The index of the match, or -1 when there is none.</returns>
        private static int FindByDeviceName(List<MonitorInfo> candidates, MonitorInfo monitor)
        {
            return candidates.FindIndex(candidate =>
                candidate.DeviceName == monitor.DeviceName
                && (monitor.HardwareId.Length == 0 || candidate.HardwareId.Length == 0));
        }

        /// <summary>
        /// Compares where and how a paired panel is presented. The device name is part of this: the app
        /// addresses monitors over DDC/CI by name, so a panel that keeps its hardware ID but moves to a
        /// different name has changed in a way that must be acted on.
        /// </summary>
        /// <param name="first">The first reading of the panel.</param>
        /// <param name="second">The second reading of the panel.</param>
        /// <returns>True when both describe the same name, rectangle, scaling and role; otherwise, false.</returns>
        private static bool HasSamePlacement(MonitorInfo first, MonitorInfo second)
        {
            return first.DeviceName == second.DeviceName
                && first.Bounds == second.Bounds
                && first.Dpi == second.Dpi
                && first.IsPrimary == second.IsPrimary
                && first.DisplayNumber == second.DisplayNumber;
        }
    }
}
