using OLED_Sleeper.Storage.Interfaces;
using System.IO;

namespace OLED_Sleeper.Storage
{
    /// <summary>
    /// Resolves the storage root from the build's distribution: <c>Data</c> beside the executable for a portable
    /// build, and <c>OLED-Sleeper</c> under the user's application data directory for an installed one.
    /// </summary>
    public class StorageRoot : IStorageRoot
    {
        /// <summary>The folder an installed build creates under the user's application data directory.</summary>
        private const string ApplicationFolderName = "OLED-Sleeper";

        /// <summary>The folder a portable build creates beside the executable.</summary>
        private const string PortableFolderName = "Data";

        public StorageRoot(IFileSystem fileSystem, bool isPortable)
        {
            DirectoryPath = isPortable
                ? Path.Combine(fileSystem.GetApplicationDirectoryPath(), PortableFolderName)
                : Path.Combine(fileSystem.GetApplicationDataPath(), ApplicationFolderName);
        }

        /// <inheritdoc />
        public string DirectoryPath { get; }
    }
}
