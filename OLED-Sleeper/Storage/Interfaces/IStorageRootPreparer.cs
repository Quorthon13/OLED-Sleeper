namespace OLED_Sleeper.Storage.Interfaces
{
    /// <summary>
    /// Creates the storage root and confirms the application can write to it.
    /// </summary>
    public interface IStorageRootPreparer
    {
        /// <summary>
        /// Creates the storage root if it is missing, then writes and deletes a file in it.
        /// </summary>
        /// <param name="error">A message naming the directory and what failed, or <c>null</c> on success.</param>
        /// <returns><c>true</c> when the directory exists and accepted the write; otherwise, <c>false</c>.</returns>
        bool TryPrepare(out string? error);
    }
}
