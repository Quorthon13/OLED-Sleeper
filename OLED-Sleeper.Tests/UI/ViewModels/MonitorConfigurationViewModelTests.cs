using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.UI.ViewModels;

namespace OLED_Sleeper.Tests.UI.ViewModels
{
    public class MonitorConfigurationViewModelTests
    {
        private readonly MonitorConfigurationViewModel _viewModel = new(new MonitorInfo { HardwareId = "MON-1" });

        [Fact]
        public void ToSettingsAndApplySettings_CarryTheVideoOption()
        {
            // Act
            _viewModel.ApplySettings(new MonitorSettings { IsActiveOnVideo = true });

            // Assert
            Assert.True(_viewModel.IsActiveOnVideo);
            Assert.True(_viewModel.ToSettings().IsActiveOnVideo);
        }

        [Fact]
        public void IsValid_WhenOnlyTheVideoOptionIsTicked_IsInvalid()
        {
            // Arrange
            ArrangeValidMonitorWithoutConditions();

            // Act
            _viewModel.IsActiveOnVideo = true;

            // Assert
            Assert.False(_viewModel.IsValid);
            Assert.Equal("At least one 'Consider Active When' option must be selected.", _viewModel.ActiveConditionsError);
        }

        [Theory]
        [InlineData(nameof(MonitorConfigurationViewModel.IsActiveOnMousePosition))]
        [InlineData(nameof(MonitorConfigurationViewModel.IsActiveOnActiveWindow))]
        [InlineData(nameof(MonitorConfigurationViewModel.IsActiveOnInput))]
        public void IsValid_WhenTheVideoOptionIsTickedWithAWakeOption_IsValid(string wakeOption)
        {
            // Arrange
            ArrangeValidMonitorWithoutConditions();
            _viewModel.IsActiveOnVideo = true;

            // Act
            typeof(MonitorConfigurationViewModel).GetProperty(wakeOption)!.SetValue(_viewModel, true);

            // Assert
            Assert.True(_viewModel.IsValid);
        }

        /// <summary>
        /// Makes the monitor managed and otherwise valid, with none of the "Consider Active When" options ticked.
        /// </summary>
        private void ArrangeValidMonitorWithoutConditions()
        {
            _viewModel.IsManaged = true;
            _viewModel.Behavior = MonitorBehaviorType.Blackout;
            _viewModel.IdleValue = 30;
            _viewModel.IsActiveOnMousePosition = false;
            _viewModel.IsActiveOnActiveWindow = false;
            _viewModel.IsActiveOnInput = false;
        }
    }
}
