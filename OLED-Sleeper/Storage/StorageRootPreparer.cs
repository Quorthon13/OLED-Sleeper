using OLED_Sleeper.Storage.Interfaces;
using System.IO;

namespace OLED_Sleeper.Storage
{
    /// <summary>
    /// Creates the storage root and proves it accepts a write by putting a file in it and deleting it again.
    /// Creating a directory that already exists succeeds whether or not it can be written to, so only the write
    /// settles it.
    /// </summary>
    public class StorageRootPreparer(IFileSystem fileSystem, IStorageRoot storageRoot) : IStorageRootPreparer
    {
        /// <summary>The file written and deleted to prove the directory accepts a write.</summary>
        private const string ProbeFileName = ".write-probe";

        /// <inheritdoc />
        public bool TryPrepare(out string? error)
        {
            try
            {
                fileSystem.CreateDirectory(storageRoot.DirectoryPath);

                var probePath = Path.Combine(storageRoot.DirectoryPath, ProbeFileName);
                fileSystem.WriteAllText(probePath, string.Empty);
                fileSystem.DeleteFile(probePath);

                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = $"OLED Sleeper cannot write to:{Environment.NewLine}{storageRoot.DirectoryPath}{Environment.NewLine}{Environment.NewLine}{ex.Message}";
                return false;
            }
        }
    }
}
