using Moq;
using OLED_Sleeper.Features.MonitorDimming.Services;
using OLED_Sleeper.Storage.Interfaces;

namespace OLED_Sleeper.Tests.Features.MonitorDimming.Services
{
    public class MonitorBrightnessStateServiceTests
    {
        private const string StateFileName = "brightness_state.json";
        private const string HardwareId = "MON-PRIMARY";

        private readonly Mock<IAppDataFileStore> _fileStore;
        private readonly MonitorBrightnessStateService _service;

        public MonitorBrightnessStateServiceTests()
        {
            _fileStore = new Mock<IAppDataFileStore>();
            _service = new MonitorBrightnessStateService(_fileStore.Object);
        }

        [Fact]
        public void LoadState_WhenTheStoreHasAState_ReturnsIt()
        {
            // Arrange
            _fileStore.Setup(s => s.Read<Dictionary<string, uint>>(StateFileName))
                .Returns(new Dictionary<string, uint> { [HardwareId] = 80 });

            // Act
            var state = _service.LoadState();

            // Assert
            Assert.Equal(80u, state[HardwareId]);
        }

        [Fact]
        public void LoadState_WhenNothingCouldBeRead_ReturnsAnEmptyDictionary()
        {
            // Arrange
            _fileStore.Setup(s => s.Read<Dictionary<string, uint>>(StateFileName)).Returns((Dictionary<string, uint>?)null);

            // Act
            var state = _service.LoadState();

            // Assert
            Assert.Empty(state);
        }

        [Fact]
        public void SaveState_WhenCalled_WritesTheStateUnderTheStateFileName()
        {
            // Arrange
            var state = new Dictionary<string, uint> { [HardwareId] = 80 };

            // Act
            _service.SaveState(state);

            // Assert
            _fileStore.Verify(s => s.TryWrite(StateFileName, state), Times.Once);
        }
    }
}
