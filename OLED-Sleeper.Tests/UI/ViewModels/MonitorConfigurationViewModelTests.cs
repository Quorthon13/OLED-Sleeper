using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.UI.ViewModels;

namespace OLED_Sleeper.Tests.UI.ViewModels
{
    public class MonitorConfigurationViewModelTests
    {
        [Fact]
        public void EasingOptions_AreEnabledOnlyForDimBehavior()
        {
            var viewModel = CreateViewModel();
            viewModel.IsEasingEnabled = true;

            Assert.False(viewModel.IsEasingOptionEnabled);
            Assert.False(viewModel.IsEaseTimersEnabled);

            viewModel.Behavior = MonitorBehaviorType.Dim;

            Assert.True(viewModel.IsEasingOptionEnabled);
            Assert.True(viewModel.IsEaseTimersEnabled);

            viewModel.Behavior = MonitorBehaviorType.Blackout;

            Assert.False(viewModel.IsEasingOptionEnabled);
            Assert.False(viewModel.IsEaseTimersEnabled);
        }

        [Fact]
        public void EasingSettings_AreSavedAndTrackedAsDirty()
        {
            var viewModel = CreateViewModel();
            viewModel.Behavior = MonitorBehaviorType.Dim;
            viewModel.IsEasingEnabled = true;
            viewModel.EaseInDurationSeconds = 1.5;
            viewModel.EaseOutDurationSeconds = 2.5;

            var settings = viewModel.ToSettings();

            Assert.True(viewModel.IsDirty);
            Assert.True(settings.IsEasingEnabled);
            Assert.Equal(1.5, settings.EaseInDurationSeconds);
            Assert.Equal(2.5, settings.EaseOutDurationSeconds);
        }

        [Fact]
        public void EasingDurations_MustUseNonNegativePointOneSecondIncrementsWhenEnabled()
        {
            var viewModel = CreateViewModel();
            viewModel.IsManaged = true;
            viewModel.Behavior = MonitorBehaviorType.Dim;
            viewModel.IsEasingEnabled = true;
            viewModel.EaseInDurationSeconds = 0.15;
            viewModel.EaseOutDurationSeconds = -0.1;

            Assert.False(viewModel.IsValid);
            Assert.Contains("increments of 0.1 seconds", viewModel.EaseInDurationError);
            Assert.Contains("zero or greater", viewModel.EaseOutDurationError);
        }

        private static MonitorConfigurationViewModel CreateViewModel()
        {
            return new MonitorConfigurationViewModel(new MonitorInfo
            {
                HardwareId = "MON-1",
                Capabilities = new DdcCiCapabilities(true, 255)
            });
        }
    }
}
