using Moq;
using OLED_Sleeper.UI.Services;
using OLED_Sleeper.UI.Services.Interfaces;
using System.Windows;

namespace OLED_Sleeper.Tests.UI.Services
{
    public class UnsavedSettingsServiceTests
    {
        private readonly Mock<IUnsavedSettings> _unsavedSettingsMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly UnsavedSettingsService _service;

        public UnsavedSettingsServiceTests()
        {
            _unsavedSettingsMock = new Mock<IUnsavedSettings>();
            _dialogServiceMock = new Mock<IDialogService>();

            _unsavedSettingsMock.Setup(x => x.IsDirty).Returns(true);
            _unsavedSettingsMock.Setup(x => x.TrySaveChanges()).Returns(true);

            _service = new UnsavedSettingsService(_unsavedSettingsMock.Object, _dialogServiceMock.Object);
        }

        [Fact]
        public void ConfirmExit_WhenNothingIsDirty_GoesAheadWithoutAsking()
        {
            // Arrange
            _unsavedSettingsMock.Setup(x => x.IsDirty).Returns(false);

            // Act
            var proceed = _service.ConfirmExit();

            // Assert
            Assert.True(proceed);
            _dialogServiceMock.Verify(x => x.AskYesNoCancel(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _unsavedSettingsMock.Verify(x => x.TrySaveChanges(), Times.Never);
            _unsavedSettingsMock.Verify(x => x.DiscardChanges(), Times.Never);
        }

        [Fact]
        public void ConfirmExit_WhenUserCancels_StopsWithoutSavingOrDiscarding()
        {
            // Arrange
            SetupAnswer(MessageBoxResult.Cancel);

            // Act
            var proceed = _service.ConfirmExit();

            // Assert
            Assert.False(proceed);
            _unsavedSettingsMock.Verify(x => x.TrySaveChanges(), Times.Never);
            _unsavedSettingsMock.Verify(x => x.DiscardChanges(), Times.Never);
        }

        [Fact]
        public void ConfirmExit_WhenUserSaves_GoesAheadAndKeepsTheChanges()
        {
            // Arrange
            SetupAnswer(MessageBoxResult.Yes);

            // Act
            var proceed = _service.ConfirmExit();

            // Assert
            Assert.True(proceed);
            _unsavedSettingsMock.Verify(x => x.TrySaveChanges(), Times.Once);
            _unsavedSettingsMock.Verify(x => x.DiscardChanges(), Times.Never);
        }

        [Fact]
        public void ConfirmExit_WhenTheSaveIsRejected_Stops()
        {
            // Arrange
            SetupAnswer(MessageBoxResult.Yes);
            _unsavedSettingsMock.Setup(x => x.TrySaveChanges()).Returns(false);

            // Act
            var proceed = _service.ConfirmExit();

            // Assert
            Assert.False(proceed);
            _unsavedSettingsMock.Verify(x => x.DiscardChanges(), Times.Never);
        }

        [Fact]
        public void ConfirmExit_WhenUserDeclinesToSave_DiscardsAndGoesAhead()
        {
            // Arrange
            SetupAnswer(MessageBoxResult.No);

            // Act
            var proceed = _service.ConfirmExit();

            // Assert
            Assert.True(proceed);
            _unsavedSettingsMock.Verify(x => x.DiscardChanges(), Times.Once);
            _unsavedSettingsMock.Verify(x => x.TrySaveChanges(), Times.Never);
        }

        [Fact]
        public void ConfirmHide_WhenUserDeclinesToSave_DiscardsAndGoesAhead()
        {
            // Arrange
            SetupAnswer(MessageBoxResult.No);

            // Act
            var proceed = _service.ConfirmHide();

            // Assert
            Assert.True(proceed);
            _unsavedSettingsMock.Verify(x => x.DiscardChanges(), Times.Once);
        }

        [Fact]
        public void ConfirmHide_WhenUserCancels_Stops()
        {
            // Arrange
            SetupAnswer(MessageBoxResult.Cancel);

            // Act
            var proceed = _service.ConfirmHide();

            // Assert
            Assert.False(proceed);
            _unsavedSettingsMock.Verify(x => x.DiscardChanges(), Times.Never);
        }

        [Fact]
        public void EachCaller_AsksAboutItsOwnAction()
        {
            // Arrange
            SetupAnswer(MessageBoxResult.No);

            // Act
            _service.ConfirmHide();
            _service.ConfirmExit();

            // Assert
            _dialogServiceMock.Verify(x => x.AskYesNoCancel(It.Is<string>(m => m.Contains("hiding the window")), "Unsaved Changes"), Times.Once);
            _dialogServiceMock.Verify(x => x.AskYesNoCancel(It.Is<string>(m => m.Contains("exiting")), "Unsaved Changes"), Times.Once);
        }

        /// <summary>
        /// Makes the unsaved-changes prompt return the supplied answer.
        /// </summary>
        /// <param name="answer">The answer the prompt should return.</param>
        private void SetupAnswer(MessageBoxResult answer)
        {
            _dialogServiceMock
                .Setup(x => x.AskYesNoCancel(It.IsAny<string>(), "Unsaved Changes"))
                .Returns(answer);
        }
    }
}
