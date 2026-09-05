using OLED_Sleeper.Storage.Interfaces;
using System.IO;

namespace OLED_Sleeper.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IFileSystem"/> over a dictionary of paths and contents, holding no real files.
    /// <see cref="Replace"/> and <see cref="Move"/> reject the same calls the Win32 originals reject, so a
    /// caller that picks the wrong one of the two fails here instead of quietly passing.
    /// <see cref="ReadsFail"/>, <see cref="WritesFail"/> and <see cref="ReplaceFails"/> script the failures
    /// the store's catch blocks exist for.
    /// </summary>
    public class FakeFileSystem : IFileSystem
    {
        private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>The root <see cref="GetApplicationDataPath"/> reports.</summary>
        public string ApplicationDataPath { get; set; } = @"C:\FakeAppData";

        /// <summary>The directory <see cref="GetApplicationDirectoryPath"/> reports.</summary>
        public string ApplicationDirectoryPath { get; set; } = @"C:\FakeApp";

        /// <summary>Every path <see cref="CreateDirectory"/> was called with, in order.</summary>
        public List<string> CreatedDirectories { get; } = new();

        /// <summary>Makes every <see cref="ReadAllText"/> call throw.</summary>
        public bool ReadsFail { get; set; }

        /// <summary>Makes every <see cref="WriteAllText"/> call throw.</summary>
        public bool WritesFail { get; set; }

        /// <summary>Makes every <see cref="Replace"/> call throw.</summary>
        public bool ReplaceFails { get; set; }

        /// <summary>Makes every <see cref="DeleteFile"/> call throw.</summary>
        public bool DeletesFail { get; set; }

        /// <summary>Makes every <see cref="CreateDirectory"/> call throw.</summary>
        public bool DirectoryCreationFails { get; set; }

        /// <summary>
        /// Puts a file in place without going through <see cref="WriteAllText"/>, so arranging a fixture is
        /// not affected by <see cref="WritesFail"/>.
        /// </summary>
        /// <param name="path">The full path of the file.</param>
        /// <param name="contents">The contents to store.</param>
        public void AddFile(string path, string contents) => _files[path] = contents;

        /// <summary>
        /// Reads a file's contents without going through <see cref="ReadAllText"/>.
        /// </summary>
        /// <param name="path">The full path of the file.</param>
        /// <returns>The contents, or <c>null</c> when no such file exists.</returns>
        public string? Contents(string path) => _files.TryGetValue(path, out var contents) ? contents : null;

        /// <inheritdoc />
        public string GetApplicationDataPath() => ApplicationDataPath;

        /// <inheritdoc />
        public string GetApplicationDirectoryPath() => ApplicationDirectoryPath;

        /// <inheritdoc />
        public void CreateDirectory(string path)
        {
            if (DirectoryCreationFails) throw new IOException($"Directory creation is failing: {path}.");

            CreatedDirectories.Add(path);
        }

        /// <inheritdoc />
        public bool FileExists(string path) => _files.ContainsKey(path);

        /// <inheritdoc />
        public string ReadAllText(string path)
        {
            if (ReadsFail) throw new IOException($"Reads are failing: {path}.");
            if (!_files.TryGetValue(path, out var contents)) throw new FileNotFoundException(path);

            return contents;
        }

        /// <inheritdoc />
        public void WriteAllText(string path, string contents)
        {
            if (WritesFail) throw new IOException($"Writes are failing: {path}.");

            _files[path] = contents;
        }

        /// <inheritdoc />
        public void Replace(string sourcePath, string destinationPath, string backupPath)
        {
            if (ReplaceFails) throw new IOException($"Replaces are failing: {destinationPath}.");
            if (!_files.ContainsKey(sourcePath)) throw new FileNotFoundException(sourcePath);
            if (!_files.ContainsKey(destinationPath)) throw new FileNotFoundException(destinationPath);

            _files[backupPath] = _files[destinationPath];
            _files[destinationPath] = _files[sourcePath];
            _files.Remove(sourcePath);
        }

        /// <inheritdoc />
        public void Move(string sourcePath, string destinationPath)
        {
            if (!_files.ContainsKey(sourcePath)) throw new FileNotFoundException(sourcePath);
            if (_files.ContainsKey(destinationPath)) throw new IOException($"Already exists: {destinationPath}.");

            _files[destinationPath] = _files[sourcePath];
            _files.Remove(sourcePath);
        }

        /// <inheritdoc />
        public void DeleteFile(string path)
        {
            if (DeletesFail) throw new IOException($"Deletes are failing: {path}.");

            _files.Remove(path);
        }
    }
}
