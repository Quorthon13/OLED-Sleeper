using Moq;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Models;
using OLED_Sleeper.Features.MonitorIdleDetection.Services;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Messaging.Interfaces;
using OLED_Sleeper.Tests.TestDoubles;
using OLED_Sleeper.UI.Models;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorIdleDetection.Services
{
    public class MonitorIdleDetectionServiceTests
    {
        private const string HardwareId = "OLED-1";
        private const int IdleSeconds = 30;

        private static readonly Rect Bounds = new(0, 0, 1920, 1080);
        private static readonly MediaDecision VideoPlaying = new(true, "fullscreen+display-required");

        private readonly Mock<IMediator> _mediator = new();
        private readonly FakeMediaActivityMonitor _media = new();
        private readonly FakeTimeProvider _time = new();
        private readonly MonitorIdleDetectionService _service;

        public MonitorIdleDetectionServiceTests()
        {
            _service = new MonitorIdleDetectionService(_mediator.Object, _media, _time);
        }

        [Fact]
        public async Task ProcessTick_WhileAVideoPlays_KeepsTheMonitorAwake()
        {
            // Arrange
            await Configure(video: true);
            _media.Decisions[HardwareId] = VideoPlaying;

            // Act
            Tick();
            Advance(TimeSpan.FromMinutes(10));
            Tick();

            // Assert
            VerifyIdleSent(Times.Never());
        }

        [Fact]
        public async Task ProcessTick_WhenTheVideoStops_StartsTheFullIdleTimeoutFromThatMoment()
        {
            // Arrange
            await Configure(video: true);
            _media.Decisions[HardwareId] = VideoPlaying;
            Tick();
            Advance(TimeSpan.FromHours(1));
            Tick();

            // Act
            _media.Decisions.Clear();
            Tick();
            Advance(TimeSpan.FromSeconds(IdleSeconds - 1));
            Tick();
            VerifyIdleSent(Times.Never());
            Advance(TimeSpan.FromSeconds(2));
            Tick();

            // Assert
            VerifyIdleSent(Times.Once());
        }

        [Fact]
        public async Task ProcessTick_WhenTheVideoOptionIsOff_IgnoresTheMediaDecision()
        {
            // Arrange
            await Configure();
            _media.Decisions[HardwareId] = VideoPlaying;

            // Act
            Tick();
            Advance(TimeSpan.FromSeconds(IdleSeconds + 1));
            Tick();

            // Assert
            VerifyIdleSent(Times.Once());
        }

        [Fact]
        public async Task ProcessTick_WhenAVideoStartsOnAMonitorThatIsAlreadyIdle_DoesNotWakeIt()
        {
            // Arrange
            await Configure(video: true);
            Tick();
            Advance(TimeSpan.FromSeconds(IdleSeconds + 1));
            Tick();
            VerifyIdleSent(Times.Once());
            _media.Calls.Clear();

            // Act
            _media.Decisions[HardwareId] = VideoPlaying;
            Tick();
            Advance(TimeSpan.FromSeconds(5));
            Tick();

            // Assert
            _mediator.Verify(m => m.SendAsync(It.IsAny<ApplyMonitorActiveBehaviorCommand>()), Times.Never);
            Assert.DoesNotContain(HardwareId, _media.RequestedIds);
        }

        [Fact]
        public async Task ProcessTick_WhenRealInputArrivesOnAnIdleMonitor_StillWakesIt()
        {
            // Arrange
            await Configure(video: true, input: true);
            Tick(idleMilliseconds: 600_000);
            Advance(TimeSpan.FromSeconds(IdleSeconds + 1));
            Tick(idleMilliseconds: 600_000);

            // Act
            Tick(idleMilliseconds: 0);

            // Assert
            _mediator.Verify(m => m.SendAsync(It.IsAny<ApplyMonitorActiveBehaviorCommand>()), Times.Once);
        }

        [Fact]
        public async Task ProcessTick_WithTheVideoOptionOff_NeverAsksTheMediaMonitorAboutIt()
        {
            // Arrange
            await Configure();

            // Act
            Tick();

            // Assert
            Assert.DoesNotContain(HardwareId, _media.RequestedIds);
        }

        [Fact]
        public async Task ProcessTick_WithTheVideoOptionOn_AsksTheMediaMonitorAboutIt()
        {
            // Arrange
            await Configure(video: true);

            // Act
            Tick();

            // Assert
            Assert.Equal(HardwareId, Assert.Single(Assert.Single(_media.Calls)).HardwareId);
        }

        [Fact]
        public async Task ProcessTick_WhenVideoHasKeptTheMonitorAwakeForTheCap_LetsItGoIdle()
        {
            // Arrange
            await Configure(video: true);
            _media.Decisions[HardwareId] = VideoPlaying;
            Tick();

            // Act
            Advance(MonitorIdleDetectionService.MaxMediaKeepAwake);
            Tick();
            Advance(TimeSpan.FromSeconds(IdleSeconds + 1));
            Tick();

            // Assert
            VerifyIdleSent(Times.Once());
        }

        [Fact]
        public async Task ProcessTick_JustBeforeTheCap_StillKeepsTheMonitorAwake()
        {
            // Arrange
            await Configure(video: true);
            _media.Decisions[HardwareId] = VideoPlaying;
            Tick();

            // Act
            Advance(MonitorIdleDetectionService.MaxMediaKeepAwake - TimeSpan.FromMinutes(1));
            Tick();
            Advance(TimeSpan.FromSeconds(IdleSeconds + 1));
            Tick();

            // Assert
            VerifyIdleSent(Times.Never());
        }

        [Fact]
        public async Task ProcessTick_WhenRealActivityHappensBeforeTheCap_RestartsTheCap()
        {
            // Arrange
            await Configure(video: true, input: true);
            _media.Decisions[HardwareId] = VideoPlaying;
            Tick(idleMilliseconds: 600_000);
            Advance(MonitorIdleDetectionService.MaxMediaKeepAwake - TimeSpan.FromMinutes(1));
            Tick(idleMilliseconds: 0);
            Advance(TimeSpan.FromMinutes(10));
            Tick(idleMilliseconds: 600_000);

            // Act
            Advance(TimeSpan.FromSeconds(IdleSeconds + 1));
            Tick(idleMilliseconds: 600_000);

            // Assert
            VerifyIdleSent(Times.Never());
        }

        /// <summary>
        /// Starts tracking one managed monitor with the given options and no other way of being active.
        /// </summary>
        private async Task Configure(bool video = false, bool input = false)
        {
            var settings = new MonitorSettings
            {
                HardwareId = HardwareId,
                IsManaged = true,
                IdleValue = IdleSeconds,
                IdleUnit = TimeUnit.Seconds,
                IsActiveOnMousePosition = false,
                IsActiveOnActiveWindow = false,
                IsActiveOnInput = input,
                IsActiveOnVideo = video
            };
            var monitor = new MonitorInfo { HardwareId = HardwareId, DisplayNumber = 2, Bounds = Bounds };

            await _service.UpdateSettingsAsync(new List<MonitorSettings> { settings }, new List<MonitorInfo> { monitor });
        }

        /// <summary>
        /// Runs one pass with the cursor and the foreground window away from the monitor.
        /// </summary>
        private void Tick(uint idleMilliseconds = 600_000)
        {
            _service.ProcessTick(new MonitorIdleDetectionService.SystemState(idleMilliseconds, new Point(-5000, -5000), Rect.Empty, nint.Zero));
        }

        private void Advance(TimeSpan amount) => _time.Advance(amount);

        private void VerifyIdleSent(Times times)
        {
            _mediator.Verify(m => m.SendAsync(It.IsAny<ApplyMonitorIdleBehaviorCommand>()), times);
        }
    }
}
