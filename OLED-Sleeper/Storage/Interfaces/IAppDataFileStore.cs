namespace OLED_Sleeper.Storage.Interfaces
{
    /// <summary>
    /// Provides methods for reading and writing JSON files in the application's data directory.
    /// </summary>
    public interface IAppDataFileStore
    {
        /// <summary>
        /// Reads a file and deserializes its contents.
        /// </summary>
        /// <typeparam name="T">The type the contents deserialize to.</typeparam>
        /// <param name="fileName">The file name, with no directory part.</param>
        /// <returns>The deserialized contents, or <c>null</c> when nothing could be read.</returns>
        T? Read<T>(string fileName);

        /// <summary>
        /// Serializes a value and writes it to a file, replacing any existing contents.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="fileName">The file name, with no directory part.</param>
        /// <param name="value">The value to write.</param>
        /// <returns><c>true</c> if the file was written; otherwise, <c>false</c>.</returns>
        bool TryWrite<T>(string fileName, T value);
    }
}
