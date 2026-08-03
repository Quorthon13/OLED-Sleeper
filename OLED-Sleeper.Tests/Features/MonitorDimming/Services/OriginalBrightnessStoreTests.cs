using Moq;
using OLED_Sleeper.Features.MonitorDimming.Services;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;

namespace OLED_Sleeper.Tests.Features.MonitorDimming.Services
{
    public class OriginalBrightnessStoreTests
    {
        private const string HardwareId = "MON-PRIMARY";

        private readonly Mock<IMonitorBrightnessStateService> _stateService;

        public OriginalBrightnessStoreTests()
        {
            _stateService = new Mock<IMonitorBrightnessStateService>();
            _stateService.Setup(s => s.LoadState()).Returns(new Dictionary<string, uint>());
        }

        [Fact]
        public void Constructor_WhenBuilt_LoadsTheRecordingsFromDisk()
        {
            // Arrange
            _stateService.Setup(s => s.LoadState()).Returns(new Dictionary<string, uint> { [HardwareId] = 80 });

            // Act
            var store = new OriginalBrightnessStore(_stateService.Object);

            // Assert
            Assert.True(store.TryGetOriginal(HardwareId, out var brightness));
            Assert.Equal(80u, brightness);
        }

        [Fact]
        public void TryGetOriginal_WhenMonitorHasNoRecording_ReturnsFalse()
        {
            // Arrange
            var store = new OriginalBrightnessStore(_stateService.Object);

            // Act
            var found = store.TryGetOriginal(HardwareId, out _);

            // Assert
            Assert.False(found);
        }

        [Fact]
        public void RecordOriginal_WhenMonitorHasNoRecording_RecordsItAndSavesTheState()
        {
            // Arrange
            var store = new OriginalBrightnessStore(_stateService.Object);

            // Act
            store.RecordOriginal(HardwareId, 80);

            // Assert
            Assert.True(store.TryGetOriginal(HardwareId, out var brightness));
            Assert.Equal(80u, brightness);
            _stateService.Verify(s => s.SaveState(It.Is<Dictionary<string, uint>>(d => d[HardwareId] == 80)), Times.Once);
        }

        [Fact]
        public void RecordOriginal_WhenMonitorAlreadyHasARecording_KeepsTheFirstAndDoesNotSaveAgain()
        {
            // Arrange
            var store = new OriginalBrightnessStore(_stateService.Object);
            store.RecordOriginal(HardwareId, 80);

            // Act
            store.RecordOriginal(HardwareId, 5);

            // Assert
            Assert.True(store.TryGetOriginal(HardwareId, out var brightness));
            Assert.Equal(80u, brightness);
            _stateService.Verify(s => s.SaveState(It.IsAny<Dictionary<string, uint>>()), Times.Once);
        }

        [Fact]
        public void RemoveOriginal_WhenMonitorHasARecording_RemovesItAndSavesTheState()
        {
            // Arrange
            var store = new OriginalBrightnessStore(_stateService.Object);
            store.RecordOriginal(HardwareId, 80);

            // Act
            store.RemoveOriginal(HardwareId);

            // Assert
            Assert.False(store.TryGetOriginal(HardwareId, out _));
            _stateService.Verify(s => s.SaveState(It.Is<Dictionary<string, uint>>(d => d.Count == 0)), Times.Once);
        }

        [Fact]
        public void RemoveOriginal_WhenMonitorHasNoRecording_DoesNotSave()
        {
            // Arrange
            var store = new OriginalBrightnessStore(_stateService.Object);

            // Act
            store.RemoveOriginal(HardwareId);

            // Assert
            _stateService.Verify(s => s.SaveState(It.IsAny<Dictionary<string, uint>>()), Times.Never);
        }

        [Fact]
        public void GetAll_WhenTheResultIsMutated_LeavesTheStoreUnchanged()
        {
            // Arrange
            var store = new OriginalBrightnessStore(_stateService.Object);
            store.RecordOriginal(HardwareId, 80);

            // Act
            store.GetAll().Clear();

            // Assert
            Assert.True(store.TryGetOriginal(HardwareId, out _));
        }
    }
}
