using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Storage.Interfaces;

namespace OLED_Sleeper.Features.MonitorDimming.Services
{
    /// <summary>
    /// Keeps the brightness state in <c>brightness_state.json</c> under the application's data directory.
    /// </summary>
    public class MonitorBrightnessStateService : IMonitorBrightnessStateService
    {
        /// <summary>The name of the file the state is kept in.</summary>
        private const string StateFileName = "brightness_state.json";

        private readonly IAppDataFileStore _fileStore;

        public MonitorBrightnessStateService(IAppDataFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        /// <inheritdoc />
        public Dictionary<string, uint> LoadState()
            => _fileStore.Read<Dictionary<string, uint>>(StateFileName) ?? new Dictionary<string, uint>();

        /// <inheritdoc />
        public void SaveState(Dictionary<string, uint> state) => _fileStore.TryWrite(StateFileName, state);
    }
}
