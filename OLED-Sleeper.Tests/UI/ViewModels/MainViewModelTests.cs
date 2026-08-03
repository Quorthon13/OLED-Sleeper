using Moq;
using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using OLED_Sleeper.Tests.TestDoubles;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;

namespace OLED_Sleeper.Tests.UI.ViewModels
{
    public class MainViewModelTests
    {
        private readonly Mock<IWorkspaceService> _workspaceServiceMock;
        private readonly Mock<IMonitorSettingsFileService> _settingsServiceMock;
        private readonly Mock<IMainWindowAccessor> _mainWindowAccessorMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly ImmediateDispatcher _dispatcher;
        private readonly MainViewModel _viewModel;

        public MainViewModelTests()
        {
            _workspaceServiceMock = new Mock<IWorkspaceService>();
            _settingsServiceMock = new Mock<IMonitorSettingsFileService>();
            _mainWindowAccessorMock = new Mock<IMainWindowAccessor>();
            _dialogServiceMock = new Mock<IDialogService>();
            _mediatorMock = new Mock<IMediator>();
            _dispatcher = new ImmediateDispatcher();

            _workspaceServiceMock
                .Setup(x => x.BuildWorkspaceAsync(It.IsAny<double>(), It.IsAny<double>()))
                .Returns(Task.CompletedTask);
            _workspaceServiceMock
                .Setup(x => x.RefreshWorkspaceAsync(It.IsAny<double>(), It.IsAny<double>()))
                .Returns(Task.CompletedTask);

            _viewModel = new MainViewModel(
                _workspaceServiceMock.Object,
                _settingsServiceMock.Object,
                _dispatcher,
                _mainWindowAccessorMock.Object,
                _dialogServiceMock.Object,
                _mediatorMock.Object);
        }

        [Fact]
        public void WorkspaceReady_PopulatesMonitors()
        {
            // Arrange
            var first = CreateMonitor("MON-1");
            var second = CreateMonitor("MON-2", 2);

            // Act
            RaiseWorkspaceReady(first, second);

            // Assert
            Assert.Equal(new[] { first, second }, _viewModel.Monitors);
        }

        [Fact]
        public void WorkspaceReady_WhenRaisedAgain_ReplacesPreviousMonitors()
        {
            // Arrange
            RaiseWorkspaceReady(CreateMonitor("MON-1"), CreateMonitor("MON-2", 2));
            var replacement = CreateMonitor("MON-3", 3);

            // Act
            RaiseWorkspaceReady(replacement);

            // Assert
            Assert.Equal(new[] { replacement }, _viewModel.Monitors);
        }

        [Fact]
        public void WorkspaceReady_WhenNotOnUiThread_MarshalsOnceAndStillPopulates()
        {
            // Arrange
            _dispatcher.IsOnUiThread = false;
            var monitor = CreateMonitor("MON-1");

            // Act
            RaiseWorkspaceReady(monitor);

            // Assert
            Assert.Equal(new[] { monitor }, _viewModel.Monitors);
            Assert.Equal(1, _dispatcher.InvokeCount);
        }

        [Fact]
        public void WorkspaceReady_WhenOnUiThread_DoesNotUseDispatcher()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");

            // Act
            RaiseWorkspaceReady(monitor);

            // Assert
            Assert.Equal(new[] { monitor }, _viewModel.Monitors);
            Assert.Equal(0, _dispatcher.InvokeCount);
        }

        [Fact]
        public void WorkspaceReady_ClearsLoadingFlag()
        {
            // Arrange
            _viewModel.RecalculateLayout(800, 600);
            Assert.True(_viewModel.IsLoading);

            // Act
            RaiseWorkspaceReady(CreateMonitor("MON-1"));

            // Assert
            Assert.False(_viewModel.IsLoading);
        }

        [Fact]
        public void WorkspaceReady_WhenSelectionPreserved_RestoresSelectedMonitorByHardwareId()
        {
            // Arrange
            var original = CreateMonitor("MON-1");
            RaiseWorkspaceReady(original, CreateMonitor("MON-2", 2));
            _viewModel.SelectMonitorCommand.Execute(original);
            _viewModel.RecalculateLayout(800, 600);
            var rebuilt = CreateMonitor("MON-1");

            // Act
            RaiseWorkspaceReady(CreateMonitor("MON-2", 2), rebuilt);

            // Assert
            Assert.Same(rebuilt, _viewModel.SelectedMonitor);
            Assert.True(rebuilt.IsSelected);
        }

        [Fact]
        public void WorkspaceReady_WhenSelectionNotPreserved_ClearsSelection()
        {
            // Arrange
            var original = CreateMonitor("MON-1");
            RaiseWorkspaceReady(original);
            _viewModel.SelectMonitorCommand.Execute(original);
            _viewModel.RecalculateLayout(800, 600);
            _viewModel.RefreshMonitors(false);

            // Act
            RaiseWorkspaceReady(CreateMonitor("MON-1"));

            // Assert
            Assert.Null(_viewModel.SelectedMonitor);
            Assert.False(_viewModel.IsMonitorSelected);
        }

        [Fact]
        public void WorkspaceReady_WhenMonitorConfigurationChanges_MarksViewModelDirty()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            RaiseWorkspaceReady(monitor);

            // Act
            monitor.Configuration.DimLevel = 50;

            // Assert
            Assert.True(_viewModel.IsDirty);
            Assert.Equal("OLED Sleeper Settings*", _viewModel.WindowTitle);
        }

        [Fact]
        public void IsDirty_WhenSetToSameValue_RaisesNoFurtherNotification()
        {
            // Arrange
            var notifications = 0;
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsDirty)) { notifications++; }
            };

            // Act
            _viewModel.IsDirty = true;
            _viewModel.IsDirty = true;

            // Assert
            Assert.Equal(1, notifications);
        }

        [Fact]
        public void SelectMonitorCommand_SelectsMonitorAndDeselectsPrevious()
        {
            // Arrange
            var first = CreateMonitor("MON-1");
            var second = CreateMonitor("MON-2", 2);
            RaiseWorkspaceReady(first, second);

            // Act
            _viewModel.SelectMonitorCommand.Execute(first);
            _viewModel.SelectMonitorCommand.Execute(second);

            // Assert
            Assert.Same(second, _viewModel.SelectedMonitor);
            Assert.False(first.IsSelected);
            Assert.True(second.IsSelected);
            Assert.True(_viewModel.IsMonitorSelected);
        }

        [Fact]
        public void SelectMonitorCommand_WithUnrelatedParameter_KeepsCurrentSelection()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            RaiseWorkspaceReady(monitor);
            _viewModel.SelectMonitorCommand.Execute(monitor);

            // Act
            _viewModel.SelectMonitorCommand.Execute("not a monitor");

            // Assert
            Assert.Same(monitor, _viewModel.SelectedMonitor);
        }

        [Fact]
        public void RecalculateLayout_RequestsWorkspaceBuildForNewSize()
        {
            // Act
            _viewModel.RecalculateLayout(800, 600);

            // Assert
            _workspaceServiceMock.Verify(x => x.BuildWorkspaceAsync(800, 600), Times.Once);
            Assert.True(_viewModel.IsLoading);
        }

        [Fact]
        public void RecalculateLayout_WithNonPositiveSize_LeavesLoadingUntouched()
        {
            // Act
            _viewModel.RecalculateLayout(0, 600);

            // Assert
            Assert.False(_viewModel.IsLoading);
            _workspaceServiceMock.Verify(x => x.BuildWorkspaceAsync(0, 600), Times.Once);
        }

        [Fact]
        public void RecalculateLayout_WhenWorkspaceTaskFaults_DoesNotThrow()
        {
            // Arrange
            _workspaceServiceMock
                .Setup(x => x.BuildWorkspaceAsync(It.IsAny<double>(), It.IsAny<double>()))
                .Returns(Task.FromException(new InvalidOperationException("Simulated workspace failure.")));

            // Act
            var exception = Record.Exception(() => _viewModel.RecalculateLayout(800, 600));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void RefreshMonitors_RequestsWorkspaceRefreshForCurrentContainerSize()
        {
            // Arrange
            _viewModel.RecalculateLayout(800, 600);

            // Act
            _viewModel.RefreshMonitors(false);

            // Assert
            _workspaceServiceMock.Verify(x => x.RefreshWorkspaceAsync(800, 600), Times.Once);
            Assert.True(_viewModel.IsLoading);
        }

        [Fact]
        public void ReloadMonitorsCommand_RequestsWorkspaceRefresh()
        {
            // Arrange
            _viewModel.RecalculateLayout(800, 600);

            // Act
            _viewModel.ReloadMonitorsCommand.Execute(null);

            // Assert
            _workspaceServiceMock.Verify(x => x.RefreshWorkspaceAsync(800, 600), Times.Once);
        }

        [Fact]
        public void DiscardChangesCommand_RefreshesWorkspacePreservingSelection()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            RaiseWorkspaceReady(monitor);
            _viewModel.SelectMonitorCommand.Execute(monitor);
            _viewModel.RecalculateLayout(800, 600);

            // Act
            _viewModel.DiscardChangesCommand.Execute(null);
            var rebuilt = CreateMonitor("MON-1");
            RaiseWorkspaceReady(rebuilt);

            // Assert
            _workspaceServiceMock.Verify(x => x.RefreshWorkspaceAsync(800, 600), Times.Once);
            Assert.Same(rebuilt, _viewModel.SelectedMonitor);
        }

        [Fact]
        public void OnWindowClosing_WhenNotDirty_HidesMainWindowAndReturnsFalse()
        {
            // Act
            var shouldClose = _viewModel.OnWindowClosing();

            // Assert
            Assert.False(shouldClose);
            _mainWindowAccessorMock.Verify(x => x.HideMainWindow(), Times.Once);
            _dialogServiceMock.Verify(x => x.AskYesNoCancel(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void OnWindowClosing_WhenDirtyAndUserCancels_KeepsWindowVisible()
        {
            // Arrange
            _viewModel.IsDirty = true;
            SetupUnsavedChangesAnswer(MessageBoxResult.Cancel);

            // Act
            var shouldClose = _viewModel.OnWindowClosing();

            // Assert
            Assert.False(shouldClose);
            _mainWindowAccessorMock.Verify(x => x.HideMainWindow(), Times.Never);
            _settingsServiceMock.Verify(x => x.SaveSettings(It.IsAny<List<MonitorSettings>>()), Times.Never);
        }

        [Fact]
        public void OnWindowClosing_WhenDirtyAndUserSaves_SavesThenHides()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            RaiseWorkspaceReady(monitor);
            monitor.Configuration.DimLevel = 50;
            SetupUnsavedChangesAnswer(MessageBoxResult.Yes);

            // Act
            var shouldClose = _viewModel.OnWindowClosing();

            // Assert
            Assert.False(shouldClose);
            _settingsServiceMock.Verify(x => x.SaveSettings(It.IsAny<List<MonitorSettings>>()), Times.Once);
            _mainWindowAccessorMock.Verify(x => x.HideMainWindow(), Times.Once);
        }

        [Fact]
        public void OnWindowClosing_WhenDirtyAndUserDiscards_HidesWithoutSaving()
        {
            // Arrange
            _viewModel.IsDirty = true;
            SetupUnsavedChangesAnswer(MessageBoxResult.No);

            // Act
            var shouldClose = _viewModel.OnWindowClosing();

            // Assert
            Assert.False(shouldClose);
            _settingsServiceMock.Verify(x => x.SaveSettings(It.IsAny<List<MonitorSettings>>()), Times.Never);
            _mainWindowAccessorMock.Verify(x => x.HideMainWindow(), Times.Once);
        }

        [Fact]
        public void SaveSettingsCommand_WhenSettingsValid_SavesAndClearsDirtyState()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            RaiseWorkspaceReady(monitor);
            monitor.Configuration.DimLevel = 50;
            Assert.True(_viewModel.IsDirty);

            List<MonitorSettings>? saved = null;
            _settingsServiceMock
                .Setup(x => x.SaveSettings(It.IsAny<List<MonitorSettings>>()))
                .Callback<List<MonitorSettings>>(settings => saved = settings);

            // Act
            _viewModel.SaveSettingsCommand.Execute(null);

            // Assert
            Assert.NotNull(saved);
            var entry = Assert.Single(saved!);
            Assert.Equal("MON-1", entry.HardwareId);
            Assert.Equal(50, entry.DimLevel);
            Assert.False(_viewModel.IsDirty);
            Assert.Equal("Saved!", _viewModel.SaveButtonText);
        }

        [Fact]
        public void SaveSettingsCommand_WhenSettingsInvalid_ShowsErrorAndDoesNotSave()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            RaiseWorkspaceReady(monitor);
            monitor.Configuration.IsManaged = true;

            // Act
            _viewModel.SaveSettingsCommand.Execute(null);

            // Assert
            _dialogServiceMock.Verify(x => x.ShowError(It.IsAny<string>(), "Monitor Configuration Error"), Times.Once);
            _settingsServiceMock.Verify(x => x.SaveSettings(It.IsAny<List<MonitorSettings>>()), Times.Never);
            Assert.Equal("Save Settings", _viewModel.SaveButtonText);
            Assert.True(_viewModel.IsDirty);
        }

        [Fact]
        public void SaveAndDiscardCommands_WhenNothingIsDirty_CannotExecute()
        {
            // Arrange
            RaiseWorkspaceReady(CreateMonitor("MON-1"));

            // Act
            var canSave = _viewModel.SaveSettingsCommand.CanExecute(null);
            var canDiscard = _viewModel.DiscardChangesCommand.CanExecute(null);

            // Assert
            Assert.False(canSave);
            Assert.False(canDiscard);
        }

        [Fact]
        public void SaveAndDiscardCommands_WhenAMonitorIsDirty_CanExecute()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            RaiseWorkspaceReady(monitor);

            // Act
            monitor.Configuration.DimLevel = 50;

            // Assert
            Assert.True(_viewModel.SaveSettingsCommand.CanExecute(null));
            Assert.True(_viewModel.DiscardChangesCommand.CanExecute(null));
        }

        /// <summary>
        /// Makes the unsaved-changes prompt return the supplied answer.
        /// </summary>
        private void SetupUnsavedChangesAnswer(MessageBoxResult answer)
        {
            _dialogServiceMock
                .Setup(x => x.AskYesNoCancel(It.IsAny<string>(), "Unsaved Changes"))
                .Returns(answer);
        }

        /// <summary>
        /// Builds a layout view model over a 1920x1080 monitor with the supplied hardware ID.
        /// </summary>
        private static MonitorLayoutViewModel CreateMonitor(string hardwareId, int displayNumber = 1)
        {
            var bounds = new Rect(0, 0, 1920, 1080);
            var monitorInfo = new MonitorInfo
            {
                HardwareId = hardwareId,
                DisplayNumber = displayNumber,
                DeviceName = $@"\\.\DISPLAY{displayNumber}",
                Bounds = bounds
            };

            return new MonitorLayoutViewModel(monitorInfo, 1.0, bounds, 0, 0);
        }

        /// <summary>
        /// Raises WorkspaceReady with the supplied monitors, as the workspace service does once a build finishes.
        /// </summary>
        private void RaiseWorkspaceReady(params MonitorLayoutViewModel[] monitors)
        {
            _workspaceServiceMock.Raise(
                x => x.WorkspaceReady += null,
                this,
                new ObservableCollection<MonitorLayoutViewModel>(monitors));
        }
    }
}
