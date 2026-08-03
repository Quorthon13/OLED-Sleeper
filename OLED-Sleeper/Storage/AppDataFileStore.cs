using OLED_Sleeper.Storage.Interfaces;
using Serilog;
using System.IO;
using System.Text.Json;

namespace OLED_Sleeper.Storage
{
    /// <summary>
    /// Reads and writes JSON files under <c>%APPDATA%\OLED-Sleeper</c>.
    /// A write is built in a temporary file and replaces the target, keeping the contents it replaced as a backup.
    /// A read tries the target and then that backup, so an interrupted write does not lose the previous contents.
    /// </summary>
    public class AppDataFileStore : IAppDataFileStore
    {
        #region Constants

        /// <summary>The folder created under the user's application data directory.</summary>
        private const string ApplicationFolderName = "OLED-Sleeper";

        /// <summary>Appended to a file name for the staging file a write is built in.</summary>
        private const string TempFileSuffix = ".tmp";

        /// <summary>Appended to a file name for the contents a write replaced.</summary>
        private const string BackupFileSuffix = ".bak";

        #endregion Constants

        #region Fields

        private readonly IFileSystem _fileSystem;

        /// <summary>The directory every file name is resolved against.</summary>
        private readonly string _storeDirectory;

        private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Resolves the store directory and creates it if it does not exist.
        /// </summary>
        public AppDataFileStore(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _storeDirectory = Path.Combine(_fileSystem.GetApplicationDataPath(), ApplicationFolderName);
            _fileSystem.CreateDirectory(_storeDirectory);
        }

        #endregion Constructor

        #region IAppDataFileStore Implementation

        /// <inheritdoc />
        public T? Read<T>(string fileName)
        {
            var filePath = Path.Combine(_storeDirectory, fileName);

            if (TryRead<T>(filePath, out var value))
            {
                return value;
            }

            if (TryRead<T>(filePath + BackupFileSuffix, out var backupValue))
            {
                Log.Warning("Loaded {FileName} from its backup file.", fileName);
                return backupValue;
            }

            return default;
        }

        /// <inheritdoc />
        public bool TryWrite<T>(string fileName, T value)
        {
            var filePath = Path.Combine(_storeDirectory, fileName);
            var tempFilePath = filePath + TempFileSuffix;

            try
            {
                _fileSystem.WriteAllText(tempFilePath, JsonSerializer.Serialize(value, SerializerOptions));

                if (_fileSystem.FileExists(filePath))
                {
                    _fileSystem.Replace(tempFilePath, filePath, filePath + BackupFileSuffix);
                }
                else
                {
                    _fileSystem.Move(tempFilePath, filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to write {FilePath}.", filePath);
                return false;
            }
        }

        #endregion IAppDataFileStore Implementation

        #region Private Methods

        /// <summary>
        /// Reads and deserializes one file.
        /// </summary>
        /// <typeparam name="T">The type the contents deserialize to.</typeparam>
        /// <param name="filePath">The full path of the file to read.</param>
        /// <param name="value">The deserialized contents, which are <c>null</c> when the file held the JSON literal <c>null</c>.</param>
        /// <returns><c>true</c> if the file was read and deserialized; otherwise, <c>false</c>.</returns>
        private bool TryRead<T>(string filePath, out T? value)
        {
            value = default;

            if (!_fileSystem.FileExists(filePath))
            {
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<T>(_fileSystem.ReadAllText(filePath));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to read {FilePath}.", filePath);
                return false;
            }
        }

        #endregion Private Methods
    }
}
