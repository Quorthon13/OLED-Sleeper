namespace OLED_Sleeper.Features.UserSettings.Models
{
    /// <summary>
    /// The contents of <c>settings.json</c>: the stored monitor settings and the schema version they were
    /// written under. A document whose version does not match the current one is discarded rather than migrated.
    /// </summary>
    public class MonitorSettingsDocument
    {
        /// <summary>
        /// The schema version the settings were written under. A file predating versioning deserializes with
        /// this left at its default of 0, which no current version ever matches.
        /// </summary>
        public int SchemaVersion { get; set; }

        /// <summary>
        /// The stored monitor settings.
        /// </summary>
        public List<MonitorSettings> Monitors { get; set; } = new();
    }
}
