namespace OLED_Sleeper.Storage.Interfaces
{
    /// <summary>
    /// Names the directory the application keeps its settings, state and logs in.
    /// </summary>
    public interface IStorageRoot
    {
        /// <summary>
        /// Gets the directory every stored file is resolved against. It is not guaranteed to exist or to be
        /// writable until <see cref="IStorageRootPreparer.TryPrepare"/> has reported success.
        /// </summary>
        string DirectoryPath { get; }
    }
}
