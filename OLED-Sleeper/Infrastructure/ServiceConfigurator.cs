using Microsoft.Extensions.DependencyInjection;
using OLED_Sleeper.Core;
using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBehavior.Handlers;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Handlers;
using OLED_Sleeper.Features.MonitorBlackout.Services;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Features.MonitorDimming.Handlers;
using OLED_Sleeper.Features.MonitorDimming.Services;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Services;
using OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Services;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.MonitorState.Commands;
using OLED_Sleeper.Features.MonitorState.Handlers;
using OLED_Sleeper.Features.MonitorState.Services;
using OLED_Sleeper.Features.MonitorState.Services.Interfaces;
using OLED_Sleeper.Features.UserSettings.Services;
using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using OLED_Sleeper.UI.Services;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using Microsoft.Extensions.Options;

namespace OLED_Sleeper.Infrastructure
{
    /// <summary>
    /// Configures and builds the application's dependency injection service provider.
    /// </summary>
    public static class ServiceConfigurator
    {
        /// <summary>
        /// Registers all application services and builds the service provider.
        /// </summary>
        /// <param name="instanceManager">The application instance manager to register as a singleton.</param>
        /// <returns>The built <see cref="IServiceProvider"/>.</returns>
        public static IServiceProvider ConfigureServices(ApplicationInstanceManager instanceManager, ApplicationOptions applicationOptions)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMediator, Mediator>();

            services.AddTransient<ICommandHandler<ApplyMonitorActiveBehaviorCommand>, ApplyMonitorActiveBehaviorCommandHandler>();
            services.AddTransient<ICommandHandler<ApplyMonitorIdleBehaviorCommand>, ApplyMonitorIdleBehaviorCommandHandler>();
            services.AddTransient<ICommandHandler<ApplyBlackoutOverlayCommand>, ApplyBlackoutOverlayCommandHandler>();
            services.AddTransient<ICommandHandler<HideBlackoutOverlayCommand>, HideBlackoutOverlayCommandHandler>();
            services.AddTransient<ICommandHandler<ApplyDimCommand>, ApplyDimCommandHandler>();
            services.AddTransient<ICommandHandler<ApplyUndimCommand>, ApplyUndimCommandHandler>();
            services.AddTransient<ICommandHandler<RestoreBrightnessOnAllMonitorsCommand>, RestoreBrightnessOnAllMonitorsCommandHandler>();
            services.AddTransient<ICommandHandler<SynchronizeMonitorStateCommand>, SynchronizeMonitorStateCommandHandler>();
            services.AddTransient<ICommandHandler<RestoreMonitorStateCommand>, RestoreMonitorStateCommandHandler>();

            services.AddSingleton(Options.Create(applicationOptions));

            services.AddSingleton<IMonitorInfoManager, MonitorInfoManager>();
            services.AddSingleton<IMonitorStateWatcher, MonitorStateWatcher>();
            services.AddSingleton<IMonitorBrightnessStateService, MonitorBrightnessStateService>();
            services.AddSingleton<IMonitorDimmingService, MonitorDimmingService>();
            services.AddSingleton<IMonitorBlackoutService, MonitorBlackoutService>();
            services.AddSingleton<IApplicationOrchestrator, ApplicationOrchestrator>();
            services.AddSingleton<IWorkspaceService, WorkspaceService>();
            services.AddSingleton<IMonitorInfoProvider, MonitorInfoProvider>();
            services.AddSingleton<IMonitorLayoutService, MonitorLayoutService>();
            services.AddSingleton<IMonitorSettingsFileService, MonitorSettingsFileService>();
            services.AddSingleton<IMonitorIdleDetectionService, MonitorIdleDetectionService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<ITrayIconService, TrayIconService>();
            services.AddSingleton<IMainWindowService, MainWindowService>();
            services.AddSingleton<IApplicationInstanceManager>(_ => instanceManager);

            return services.BuildServiceProvider();
        }
    }
}