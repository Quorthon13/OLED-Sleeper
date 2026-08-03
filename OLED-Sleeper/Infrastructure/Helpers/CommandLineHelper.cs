using CommandLine;
using OLED_Sleeper.Infrastructure.Hosting;

namespace OLED_Sleeper.Infrastructure.Helpers
{
    /// <summary>
    /// Provides helper functionality for parsing command-line arguments
    /// passed to the OLED Sleeper application.
    /// </summary>
    public static class CommandLineHelper
    {
        /// <summary>
        /// Parses the provided command-line arguments into an
        /// <see cref="ApplicationOptions"/> instance.
        /// </summary>
        /// <param name="args">
        /// The raw command-line arguments passed to the application entry point.
        /// </param>
        /// <returns>
        /// An <see cref="ApplicationOptions"/> object containing the parsed values.
        /// If parsing fails or no arguments are provided, default option values are returned.
        /// </returns>
        public static ApplicationOptions ParseArguments(string[] args)
        {
            var resultOptions = new ApplicationOptions();

            Parser.Default.ParseArguments<ApplicationOptions>(args)
                  .WithParsed(options => resultOptions = options);

            return resultOptions;
        }
    }
}