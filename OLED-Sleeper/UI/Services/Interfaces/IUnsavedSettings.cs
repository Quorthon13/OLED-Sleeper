namespace OLED_Sleeper.UI.Services.Interfaces
{
    /// <summary>
    /// The monitor settings currently being edited, and what can be done with the edits.
    /// Every member must be called on the UI thread.
    /// </summary>
    public interface IUnsavedSettings
    {
        /// <summary>
        /// Whether any monitor has changes that have not been written to disk.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Validates the current settings and writes them.
        /// </summary>
        /// <returns>False when validation rejected the settings and nothing was written.</returns>
        bool TrySaveChanges();

        /// <summary>
        /// Returns every monitor to the values it last had on disk.
        /// </summary>
        void DiscardChanges();
    }
}
