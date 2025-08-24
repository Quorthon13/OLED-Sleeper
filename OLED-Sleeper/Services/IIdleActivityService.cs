using OLED_Sleeper.Events;
using OLED_Sleeper.Models;
using System;
using System.Collections.Generic;

namespace OLED_Sleeper.Services
{
    public interface IIdleActivityService
    {
        event EventHandler<MonitorStateEventArgs> MonitorBecameIdle;

        event EventHandler<MonitorStateEventArgs> MonitorBecameActive;

        void Start();

        void Stop();

        void UpdateSettings(List<MonitorSettings> monitorSettings);
    }
}