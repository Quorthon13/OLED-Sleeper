using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;

namespace OLED_Sleeper.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IDdcCiAccess"/> over a set of fake panels, keyed by device name. A device with no
    /// panel behind it reports as unreachable. Each device keeps one session across every open, so its
    /// brightness survives and its disposals can be counted.
    /// </summary>
    public class FakeDdcCiAccess : IDdcCiAccess
    {
        private readonly Dictionary<string, FakeDdcCiSession> _panels = new();

        /// <summary>
        /// Every device name <see cref="OpenSession"/> was called with, in order, including ones that
        /// had no panel behind them.
        /// </summary>
        public List<string> OpenAttempts { get; } = new();

        /// <summary>
        /// Runs at the start of every <see cref="OpenSession"/> call, before the panel is looked up.
        /// A test can block here to hold an operation open inside the monitor's gate.
        /// </summary>
        public Action<string>? OnOpen { get; set; }

        /// <summary>
        /// Adds a reachable panel for the given device name.
        /// </summary>
        /// <param name="deviceName">The display device name.</param>
        /// <param name="brightness">The brightness the panel reports before anything is written.</param>
        /// <returns>The panel, so a test can script its failures and read what was written to it.</returns>
        public FakeDdcCiSession AddPanel(string deviceName, uint brightness)
        {
            var panel = new FakeDdcCiSession(brightness);
            _panels[deviceName] = panel;
            return panel;
        }

        /// <inheritdoc />
        public IDdcCiSession? OpenSession(string deviceName)
        {
            OpenAttempts.Add(deviceName);
            OnOpen?.Invoke(deviceName);

            return _panels.TryGetValue(deviceName, out var panel) ? panel : null;
        }
    }
}
