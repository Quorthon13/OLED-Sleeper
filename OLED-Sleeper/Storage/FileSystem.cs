using OLED_Sleeper.Storage.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace OLED_Sleeper.Storage
{
    /// <summary>
    /// Forwards every call to <see cref="File"/>, <see cref="Directory"/> and <see cref="Environment"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class FileSystem : IFileSystem
    {
        /// <inheritdoc />
        public string GetApplicationDataPath() => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <inheritdoc />
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        /// <inheritdoc />
        public bool FileExists(string path) => File.Exists(path);

        /// <inheritdoc />
        public string ReadAllText(string path) => File.ReadAllText(path);

        /// <inheritdoc />
        public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);

        /// <inheritdoc />
        public void Replace(string sourcePath, string destinationPath, string backupPath)
            => File.Replace(sourcePath, destinationPath, backupPath, ignoreMetadataErrors: true);

        /// <inheritdoc />
        public void Move(string sourcePath, string destinationPath) => File.Move(sourcePath, destinationPath);
    }
}
