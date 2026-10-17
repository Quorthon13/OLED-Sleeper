using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Features.UserSettings.Services;
using OLED_Sleeper.Storage;
using OLED_Sleeper.Tests.TestDoubles;
using System.Text.Json;

namespace OLED_Sleeper.Tests.Features.UserSettings.Services
{
    public class MonitorSettingsCompatibilityTests
    {
        private const string SettingsPath = @"C:\FakeAppData\OLED-Sleeper\settings.json";

        private readonly FakeFileSystem _fileSystem = new();
        private readonly MonitorSettingsFileService _service;

        public MonitorSettingsCompatibilityTests()
        {
            _service = new MonitorSettingsFileService(new AppDataFileStore(_fileSystem, new StorageRoot(_fileSystem, isPortable: false)));
        }

        [Fact]
        public void LoadSettings_ForAFileWrittenBeforeTheVideoOption_LoadsItWithTheOptionOff()
        {
            // Arrange
            _fileSystem.AddFile(SettingsPath, """
                {
                  "SchemaVersion": 1,
                  "Monitors": [
                    { "HardwareId": "OLED-1", "IsManaged": true, "IdleValue": 5, "IsActiveOnInput": true, "IsActiveOnMousePosition": false }
                  ]
                }
                """);

            // Act
            var settings = Assert.Single(_service.LoadSettings());

            // Assert
            Assert.True(settings.IsManaged);
            Assert.Equal(5, settings.IdleValue);
            Assert.True(settings.IsActiveOnInput);
            Assert.False(settings.IsActiveOnMousePosition);
            Assert.False(settings.IsActiveOnVideo);
        }

        [Theory]
        [InlineData("""{ "IsActiveOnFullscreenVideo": true, "IsActiveOnVideo": false }""", true)]
        [InlineData("""{ "IsActiveOnVideo": false, "IsActiveOnFullscreenVideo": true }""", true)]
        [InlineData("""{ "IsActiveOnFullscreenVideo": false, "IsActiveOnVideo": true }""", true)]
        [InlineData("""{ "IsActiveOnFullscreenVideo": false, "IsActiveOnVideo": false }""", false)]
        public void LoadSettings_ForAFileWithTheOldFullscreenOption_TurnsTheVideoOptionOnWhenEitherWasOn(string monitorJson, bool expected)
        {
            // Arrange
            _fileSystem.AddFile(SettingsPath, $$"""{ "SchemaVersion": 1, "Monitors": [ {{monitorJson}} ] }""");

            // Act
            var settings = Assert.Single(_service.LoadSettings());

            // Assert
            Assert.Equal(expected, settings.IsActiveOnVideo);
        }

        [Fact]
        public void Serialize_NeverWritesTheOldFullscreenOption()
        {
            Assert.DoesNotContain("IsActiveOnFullscreenVideo", JsonSerializer.Serialize(new MonitorSettings { IsActiveOnVideo = true }));
        }
    }
}
