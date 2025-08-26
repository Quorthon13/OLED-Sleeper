using System;
using System.Collections.Generic;
using OLED_Sleeper.Events;
using OLED_Sleeper.Models;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    /// <summary>
    /// Defines the contract for monitor idle detection services.
    /// Handles detection of idle/active state transitions and updates settings for managed monitors.
    /// </summary>
    public interface IMonitorIdleDetectionService
    {
        /// <summary>
        /// Raised when a managed monitor transitions from active to idle.
        /// </summary>
        event EventHandler<MonitorStateEventArgs> MonitorBecameIdle;

        /// <summary>
        /// Raised when a managed monitor transitions from idle to active.
        /// </summary>
        event EventHandler<MonitorStateEventArgs> MonitorBecameActive;

        /// <summary>
        /// Starts the idle detection service and begins monitoring.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the idle detection service and monitoring.
        /// </summary>
        void Stop();

        /// <summary>
        /// Updates the settings for all managed monitors.
        /// </summary>
        /// <param name="monitorSettings">The list of monitor settings to manage.</param>
        void UpdateSettings(List<MonitorSettings> monitorSettings);
    }
}