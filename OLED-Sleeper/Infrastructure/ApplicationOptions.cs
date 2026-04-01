using CommandLine;

namespace OLED_Sleeper.Infrastructure
{
    /// <summary>
    /// Represents command-line options supported by the application.
    /// These options are parsed at application startup and control runtime behavior.
    /// </summary>
    public class ApplicationOptions
    {
        /// <summary>
        /// When specified, the application starts hidden in the system tray instead
        /// of opening the main window. Intended primarily for launches performed
        /// during system startup.
        /// </summary>
        [Option('h', "hide", Required = false, HelpText = "Start the application hidden in the system tray.")]
        public bool StartHidden { get; set; }
    }
}