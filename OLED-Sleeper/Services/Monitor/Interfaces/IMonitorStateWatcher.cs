using System;
using System.Collections.Generic;
using OLED_Sleeper.Models;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    /// <summary>
    /// Provides an interface for a service that watches for changes in the set of connected monitors.
    /// </summary>
    public interface IMonitorStateWatcher : IDisposable
    {
        /// <summary>
        /// Occurs when the set of connected monitors changes.
        /// </summary>
        event EventHandler<IReadOnlyList<MonitorInfo>> MonitorsChanged;

        /// <summary>
        /// Starts monitoring for monitor state changes.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops monitoring for monitor state changes.
        /// </summary>
        void Stop();
    }
}