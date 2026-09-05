using Moq;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Features.UserSettings.Services;
using OLED_Sleeper.Storage.Interfaces;

namespace OLED_Sleeper.Tests.Features.UserSettings.Services
{
    public class MonitorSettingsFileServiceTests
    {
        private const string SettingsFileName = "settings.json";
        private const int CurrentSchemaVersion = 1;

        private readonly Mock<IAppDataFileStore> _fileStore;
        private readonly MonitorSettingsFileService _service;

        public MonitorSettingsFileServiceTests()
        {
            _fileStore = new Mock<IAppDataFileStore>();
            _fileStore.Setup(s => s.TryWrite(SettingsFileName, It.IsAny<MonitorSettingsDocument>())).Returns(true);
            _service = new MonitorSettingsFileService(_fileStore.Object);
        }

        [Fact]
        public void LoadSettings_WhenTheStoreHasSettings_ReturnsThem()
        {
            // Arrange
            SetupStoredSettings(Settings("A"));

            // Act
            var settings = _service.LoadSettings();

            // Assert
            Assert.Equal("A", Assert.Single(settings).HardwareId);
        }

        [Fact]
        public void LoadSettings_WhenNothingCouldBeRead_ReturnsAnEmptyList()
        {
            // Act
            var settings = _service.LoadSettings();

            // Assert
            Assert.Empty(settings);
        }

        [Fact]
        public void SaveSettings_WhenAStoredMonitorWasNotSupplied_KeepsItsSettings()
        {
            // Arrange
            SetupStoredSettings(Settings("A"), Settings("B"));

            // Act
            _service.SaveSettings(new List<MonitorSettings> { Settings("A") });

            // Assert
            VerifyWritten(written => written.Count == 2 && written.Any(m => m.HardwareId == "B"));
        }

        [Fact]
        public void SaveSettings_WhenAStoredMonitorWasSupplied_WritesOnlyTheSuppliedCopy()
        {
            // Arrange
            SetupStoredSettings(Settings("A", dimLevel: 15));

            // Act
            _service.SaveSettings(new List<MonitorSettings> { Settings("A", dimLevel: 50) });

            // Assert
            VerifyWritten(written => written.Count == 1 && written.Single().DimLevel == 50);
        }

        [Fact]
        public void SaveSettings_WhenNothingIsStored_WritesTheSuppliedSettings()
        {
            // Act
            _service.SaveSettings(new List<MonitorSettings> { Settings("A") });

            // Assert
            VerifyWritten(written => written.Count == 1 && written.Single().HardwareId == "A");
        }

        [Fact]
        public void SaveSettings_WhenTheWriteSucceeds_RaisesSettingsChangedWithTheSuppliedSettings()
        {
            // Arrange
            SetupStoredSettings(Settings("A"), Settings("B"));
            var supplied = new List<MonitorSettings> { Settings("A") };
            List<MonitorSettings>? raised = null;
            _service.SettingsChanged += settings => raised = settings;

            // Act
            _service.SaveSettings(supplied);

            // Assert
            Assert.Same(supplied, raised);
        }

        [Fact]
        public void SaveSettings_WhenTheWriteFails_DoesNotRaiseSettingsChanged()
        {
            // Arrange
            _fileStore.Setup(s => s.TryWrite(SettingsFileName, It.IsAny<MonitorSettingsDocument>())).Returns(false);
            var raised = false;
            _service.SettingsChanged += _ => raised = true;

            // Act
            _service.SaveSettings(new List<MonitorSettings> { Settings("A") });

            // Assert
            Assert.False(raised);
        }

        [Fact]
        public void SaveSettings_WhenASubscriberThrows_DoesNotPropagate()
        {
            // Arrange
            _service.SettingsChanged += _ => throw new InvalidOperationException("Subscriber failed.");

            // Act
            var exception = Record.Exception(() => _service.SaveSettings(new List<MonitorSettings> { Settings("A") }));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void LoadSettings_WhenTheStoredVersionIsOlder_DiscardsTheSettings()
        {
            // Arrange
            SetupStoredDocument(new MonitorSettingsDocument
            {
                SchemaVersion = CurrentSchemaVersion - 1,
                Monitors = new List<MonitorSettings> { Settings("A") }
            });

            // Act
            var settings = _service.LoadSettings();

            // Assert
            Assert.Empty(settings);
        }

        [Fact]
        public void LoadSettings_WhenTheStoredVersionIsAbsent_DiscardsTheSettings()
        {
            // Arrange
            SetupStoredDocument(new MonitorSettingsDocument { Monitors = new List<MonitorSettings> { Settings("A") } });

            // Act
            var settings = _service.LoadSettings();

            // Assert
            Assert.Empty(settings);
        }

        [Fact]
        public void SaveSettings_StampsTheCurrentSchemaVersion()
        {
            // Act
            _service.SaveSettings(new List<MonitorSettings> { Settings("A") });

            // Assert
            _fileStore.Verify(
                s => s.TryWrite(SettingsFileName, It.Is<MonitorSettingsDocument>(d => d.SchemaVersion == CurrentSchemaVersion)),
                Times.Once);
        }

        /// <summary>
        /// Builds a settings entry for the given monitor.
        /// </summary>
        private static MonitorSettings Settings(string hardwareId, double dimLevel = 15)
            => new() { HardwareId = hardwareId, DimLevel = dimLevel };

        /// <summary>
        /// Makes the store report the given entries as already on disk.
        /// </summary>
        private void SetupStoredSettings(params MonitorSettings[] stored)
            => SetupStoredDocument(new MonitorSettingsDocument { SchemaVersion = CurrentSchemaVersion, Monitors = stored.ToList() });

        /// <summary>
        /// Makes the store report the given document as already on disk.
        /// </summary>
        private void SetupStoredDocument(MonitorSettingsDocument document)
            => _fileStore.Setup(s => s.Read<MonitorSettingsDocument>(SettingsFileName)).Returns(document);

        /// <summary>
        /// Asserts that exactly one write happened and that the list it carried satisfies the predicate.
        /// </summary>
        private void VerifyWritten(Func<List<MonitorSettings>, bool> predicate)
            => _fileStore.Verify(s => s.TryWrite(SettingsFileName, It.Is<MonitorSettingsDocument>(w => predicate(w.Monitors))), Times.Once);
    }
}
