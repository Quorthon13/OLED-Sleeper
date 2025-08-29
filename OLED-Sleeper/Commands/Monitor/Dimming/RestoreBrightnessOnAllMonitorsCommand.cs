using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Commands.Monitor.Dimming
{
    /// <summary>
    /// Command to restore brightness for all monitors that were left dimmed from a previous session.
    /// </summary>
    public class RestoreBrightnessOnAllMonitorsCommand : ICommand
    {
    }
}