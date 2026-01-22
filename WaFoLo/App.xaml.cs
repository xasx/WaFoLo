using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WaFoLo.Services;
using WaFoLo.Utilities;

namespace WaFoLo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register services
            services.AddSingleton<ILogMonitorServiceFactory, LogMonitorServiceFactory>();
            services.AddSingleton<IProcessDetectionService, ProcessDetectionService>();
            services.AddSingleton<IRebootService, RebootService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<ITimestampParserFactory, TimestampParserFactory>();
            services.AddSingleton<IFileLoggerService, FileLoggerService>();

            // Register main window
            services.AddTransient<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = _serviceProvider?.GetService<MainWindow>();
            mainWindow?.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
