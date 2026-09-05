namespace OLED_Sleeper.Storage.Interfaces
{
    /// <summary>
    /// Wraps the file and directory calls the application makes, so callers can be tested without touching a disk.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Gets the roaming application data directory for the current user.
        /// </summary>
        /// <returns>The directory path, such as <c>C:\Users\Name\AppData\Roaming</c>.</returns>
        string GetApplicationDataPath();

        /// <summary>
        /// Gets the directory the running executable lives in.
        /// </summary>
        /// <returns>The directory path, without a trailing separator.</returns>
        string GetApplicationDirectoryPath();

        /// <summary>
        /// Creates a directory and any missing parent. Does nothing when the directory already exists.
        /// </summary>
        /// <param name="path">The directory to create.</param>
        void CreateDirectory(string path);

        /// <summary>
        /// Determines whether a file exists.
        /// </summary>
        /// <param name="path">The file to check.</param>
        /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
        bool FileExists(string path);

        /// <summary>
        /// Reads the entire contents of a file.
        /// </summary>
        /// <param name="path">The file to read.</param>
        /// <returns>The file's contents.</returns>
        string ReadAllText(string path);

        /// <summary>
        /// Writes text to a file, overwriting any existing contents.
        /// </summary>
        /// <param name="path">The file to write.</param>
        /// <param name="contents">The text to write.</param>
        void WriteAllText(string path, string contents);

        /// <summary>
        /// Replaces one file with another, moving the replaced file's previous contents to a backup path.
        /// The destination must already exist.
        /// </summary>
        /// <param name="sourcePath">The file that replaces the destination. The operation removes it.</param>
        /// <param name="destinationPath">The file being replaced.</param>
        /// <param name="backupPath">Where the destination's previous contents are kept.</param>
        void Replace(string sourcePath, string destinationPath, string backupPath);

        /// <summary>
        /// Moves a file to a new path.
        /// </summary>
        /// <param name="sourcePath">The file to move.</param>
        /// <param name="destinationPath">Where to move it to.</param>
        void Move(string sourcePath, string destinationPath);

        /// <summary>
        /// Deletes a file. Does nothing when the file does not exist.
        /// </summary>
        /// <param name="path">The file to delete.</param>
        void DeleteFile(string path);
    }
}
