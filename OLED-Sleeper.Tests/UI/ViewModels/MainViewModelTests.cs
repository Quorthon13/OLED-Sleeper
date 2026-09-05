using Moq;
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
        private readonly Mock<IMonitorSettingsSaveService> _saveServiceMock;
        private readonly ImmediateDispatcher _dispatcher;
        private readonly MainViewModel _viewModel;

        public MainViewModelTests()
        {
            _workspaceServiceMock = new Mock<IWorkspaceService>();
            _saveServiceMock = new Mock<IMonitorSettingsSaveService>();
            _dispatcher = new ImmediateDispatcher();

            SetupWorkspace();
            SetupSaveOutcome(true);

            _viewModel = new MainViewModel(
                _workspaceServiceMock.Object,
                _saveServiceMock.Object,
                _dispatcher);
        }

        [Fact]
        public void RecalculateLayout_PopulatesMonitors()
        {
            // Arrange
            var first = CreateMonitor("MON-1");
            var second = CreateMonitor("MON-2", 2);

            // Act
            LoadMonitors(first, second);

            // Assert
            Assert.Equal(new[] { first, second }, _viewModel.Monitors);
        }

        [Fact]
        public void RecalculateLayout_WhenBuiltAgain_ReplacesPreviousMonitors()
        {
            // Arrange
            LoadMonitors(CreateMonitor("MON-1"), CreateMonitor("MON-2", 2));
            var replacement = CreateMonitor("MON-3", 3);

            // Act
            LoadMonitors(replacement);

            // Assert
            Assert.Equal(new[] { replacement }, _viewModel.Monitors);
        }

        [Fact]
        public void RecalculateLayout_WhenNotOnUiThread_MarshalsOnceAndStillPopulates()
        {
            // Arrange
            _dispatcher.IsOnUiThread = false;
            var monitor = CreateMonitor("MON-1");

            // Act
            LoadMonitors(monitor);

            // Assert
            Assert.Equal(new[] { monitor }, _viewModel.Monitors);
            Assert.Equal(1, _dispatcher.InvokeCount);
        }

        [Fact]
        public void RecalculateLayout_WhenOnUiThread_DoesNotUseDispatcher()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");

            // Act
            LoadMonitors(monitor);

            // Assert
            Assert.Equal(new[] { monitor }, _viewModel.Monitors);
            Assert.Equal(0, _dispatcher.InvokeCount);
        }

        [Fact]
        public async Task RecalculateLayout_SetsLoadingWhileBuildingAndClearsItWhenDone()
        {
            // Arrange
            var pendingBuild = SetupPendingBuild();

            // Act
            _viewModel.RecalculateLayout(800, 600);
            var loadingWhileBuilding = _viewModel.IsLoading;
            pendingBuild.SetResult(Collect(CreateMonitor("MON-1")));
            await pendingBuild.Task;

            // Assert
            Assert.True(loadingWhileBuilding);
            Assert.False(_viewModel.IsLoading);
        }

        [Fact]
        public async Task RecalculateLayout_WhenSupersededByANewerBuild_DiscardsTheStaleResult()
        {
            // Arrange
            var stale = CreateMonitor("STALE");
            var current = CreateMonitor("CURRENT");
            var pendingBuild = SetupPendingBuild();
            _viewModel.RecalculateLayout(800, 600);

            // Act
            SetupWorkspace(current);
            _viewModel.RecalculateLayout(1024, 768);
            pendingBuild.SetResult(Collect(stale));
            await pendingBuild.Task;

            // Assert
            Assert.Equal(new[] { current }, _viewModel.Monitors);
            Assert.False(_viewModel.IsLoading);
        }

        [Fact]
        public void RecalculateLayout_WhenSelectionPreserved_RestoresSelectedMonitorByHardwareId()
        {
            // Arrange
            var original = CreateMonitor("MON-1");
            LoadMonitors(original, CreateMonitor("MON-2", 2));
            _viewModel.SelectMonitorCommand.Execute(original);
            var rebuilt = CreateMonitor("MON-1");

            // Act
            LoadMonitors(CreateMonitor("MON-2", 2), rebuilt);

            // Assert
            Assert.Same(rebuilt, _viewModel.SelectedMonitor);
            Assert.True(rebuilt.IsSelected);
        }

        [Fact]
        public void RefreshMonitors_WhenSelectionNotPreserved_ClearsSelection()
        {
            // Arrange
            var original = CreateMonitor("MON-1");
            LoadMonitors(original);
            _viewModel.SelectMonitorCommand.Execute(original);
            SetupWorkspace(CreateMonitor("MON-1"));

            // Act
            _viewModel.RefreshMonitors(false);

            // Assert
            Assert.Null(_viewModel.SelectedMonitor);
            Assert.False(_viewModel.IsMonitorSelected);
        }

        [Fact]
        public void RecalculateLayout_WhenMonitorConfigurationChanges_MarksViewModelDirty()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            LoadMonitors(monitor);

            // Act
            monitor.Configuration.DimLevel = 50;

            // Assert
            Assert.True(_viewModel.IsDirty);
            Assert.Equal("OLED Sleeper Settings*", _viewModel.WindowTitle);
        }

        [Fact]
        public void RecalculateLayout_WhenAReplacedMonitorChanges_LeavesTheViewModelClean()
        {
            // Arrange
            var replaced = CreateMonitor("MON-1");
            LoadMonitors(replaced);
            LoadMonitors(CreateMonitor("MON-2", 2));

            // Act
            replaced.Configuration.DimLevel = 50;

            // Assert
            Assert.False(_viewModel.IsDirty);
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
            LoadMonitors(first, second);

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
            LoadMonitors(monitor);
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
        }

        [Fact]
        public void RecalculateLayout_WithNonPositiveSize_RequestsNoBuild()
        {
            // Act
            _viewModel.RecalculateLayout(0, 600);

            // Assert
            Assert.False(_viewModel.IsLoading);
            _workspaceServiceMock.Verify(
                x => x.BuildWorkspaceAsync(It.IsAny<double>(), It.IsAny<double>()),
                Times.Never);
        }

        [Fact]
        public void RecalculateLayout_WhenWorkspaceTaskFaults_DoesNotThrowAndClearsLoading()
        {
            // Arrange
            _workspaceServiceMock
                .Setup(x => x.BuildWorkspaceAsync(It.IsAny<double>(), It.IsAny<double>()))
                .Returns(Task.FromException<ObservableCollection<MonitorLayoutViewModel>>(
                    new InvalidOperationException("Simulated workspace failure.")));

            // Act
            var exception = Record.Exception(() => _viewModel.RecalculateLayout(800, 600));

            // Assert
            Assert.Null(exception);
            Assert.False(_viewModel.IsLoading);
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
        public void DiscardChangesCommand_RevertsEachMonitorAndKeepsTheSelection()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            LoadMonitors(monitor);
            _viewModel.SelectMonitorCommand.Execute(monitor);
            var savedDimLevel = monitor.Configuration.DimLevel;
            monitor.Configuration.DimLevel = 50;

            // Act
            _viewModel.DiscardChangesCommand.Execute(null);

            // Assert
            Assert.Equal(savedDimLevel, monitor.Configuration.DimLevel);
            Assert.False(_viewModel.IsDirty);
            Assert.Same(monitor, _viewModel.SelectedMonitor);
            _workspaceServiceMock.Verify(x => x.RefreshWorkspaceAsync(It.IsAny<double>(), It.IsAny<double>()), Times.Never);
        }

        [Fact]
        public void TrySaveChanges_WhenSaveSucceeds_ClearsDirtyStateWithoutTouchingTheButton()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            LoadMonitors(monitor);
            monitor.Configuration.DimLevel = 50;

            // Act
            var saved = _viewModel.TrySaveChanges();

            // Assert
            Assert.True(saved);
            Assert.False(_viewModel.IsDirty);
            Assert.Equal("Save Settings", _viewModel.SaveButtonText);
        }

        [Fact]
        public void TrySaveChanges_WhenSaveIsRejected_ReportsFailureAndKeepsDirtyState()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            LoadMonitors(monitor);
            monitor.Configuration.DimLevel = 50;
            SetupSaveOutcome(false);

            // Act
            var saved = _viewModel.TrySaveChanges();

            // Assert
            Assert.False(saved);
            Assert.True(_viewModel.IsDirty);
        }

        [Fact]
        public void SaveSettingsCommand_WhenSaveSucceeds_ClearsDirtyStateAndConfirmsOnTheButton()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            LoadMonitors(monitor);
            monitor.Configuration.DimLevel = 50;
            Assert.True(_viewModel.IsDirty);

            // Act
            _viewModel.SaveSettingsCommand.Execute(null);

            // Assert
            _saveServiceMock.Verify(x => x.TrySave(_viewModel.Monitors), Times.Once);
            Assert.False(_viewModel.IsDirty);
            Assert.Equal("Saved!", _viewModel.SaveButtonText);
        }

        [Fact]
        public void SaveSettingsCommand_WhenSaveIsRejected_KeepsDirtyStateAndLeavesTheButtonAlone()
        {
            // Arrange
            var monitor = CreateMonitor("MON-1");
            LoadMonitors(monitor);
            monitor.Configuration.DimLevel = 50;
            SetupSaveOutcome(false);

            // Act
            _viewModel.SaveSettingsCommand.Execute(null);

            // Assert
            Assert.Equal("Save Settings", _viewModel.SaveButtonText);
            Assert.True(_viewModel.IsDirty);
        }

        [Fact]
        public void SaveAndDiscardCommands_WhenNothingIsDirty_CannotExecute()
        {
            // Arrange
            LoadMonitors(CreateMonitor("MON-1"));

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
            LoadMonitors(monitor);

            // Act
            monitor.Configuration.DimLevel = 50;

            // Assert
            Assert.True(_viewModel.SaveSettingsCommand.CanExecute(null));
            Assert.True(_viewModel.DiscardChangesCommand.CanExecute(null));
        }

        /// <summary>
        /// Makes both workspace builds return the supplied monitors as soon as they are awaited.
        /// </summary>
        /// <param name="monitors">The monitors each build should produce.</param>
        private void SetupWorkspace(params MonitorLayoutViewModel[] monitors)
        {
            _workspaceServiceMock
                .Setup(x => x.BuildWorkspaceAsync(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(() => Collect(monitors));
            _workspaceServiceMock
                .Setup(x => x.RefreshWorkspaceAsync(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(() => Collect(monitors));
        }

        /// <summary>
        /// Makes the next build hang until the returned source is completed, so a second build can
        /// be started while the first is still in flight.
        /// </summary>
        /// <returns>The source that completes the pending build.</returns>
        private TaskCompletionSource<ObservableCollection<MonitorLayoutViewModel>> SetupPendingBuild()
        {
            var pending = new TaskCompletionSource<ObservableCollection<MonitorLayoutViewModel>>();
            _workspaceServiceMock
                .Setup(x => x.BuildWorkspaceAsync(It.IsAny<double>(), It.IsAny<double>()))
                .Returns(pending.Task);

            return pending;
        }

        /// <summary>
        /// Builds the workspace with the supplied monitors and applies the result, as a resize does.
        /// </summary>
        /// <param name="monitors">The monitors the build should produce.</param>
        private void LoadMonitors(params MonitorLayoutViewModel[] monitors)
        {
            SetupWorkspace(monitors);
            _viewModel.RecalculateLayout(800, 600);
        }

        /// <summary>
        /// Makes the save service accept or reject the settings. A successful save marks each monitor
        /// as saved, as the real service does.
        /// </summary>
        /// <param name="succeeds">Whether the save service should report the settings as written.</param>
        private void SetupSaveOutcome(bool succeeds)
        {
            _saveServiceMock
                .Setup(x => x.TrySave(It.IsAny<IReadOnlyList<MonitorLayoutViewModel>>()))
                .Returns<IReadOnlyList<MonitorLayoutViewModel>>(monitors =>
                {
                    if (!succeeds) return false;

                    foreach (var monitor in monitors)
                    {
                        monitor.Configuration.MarkAsSaved();
                    }

                    return true;
                });
        }

        /// <summary>
        /// Wraps the supplied monitors in the collection type a workspace build returns.
        /// </summary>
        /// <param name="monitors">The monitors to wrap.</param>
        /// <returns>A new collection holding the supplied monitors.</returns>
        private static ObservableCollection<MonitorLayoutViewModel> Collect(params MonitorLayoutViewModel[] monitors)
        {
            return new ObservableCollection<MonitorLayoutViewModel>(monitors);
        }

        /// <summary>
        /// Builds a layout view model over a 1920x1080 monitor with the supplied hardware ID.
        /// </summary>
        /// <param name="hardwareId">The hardware ID for the monitor.</param>
        /// <param name="displayNumber">The display number for the monitor.</param>
        /// <returns>A layout view model over the described monitor.</returns>
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
    }
}
