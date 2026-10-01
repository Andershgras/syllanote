using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Syllanote.Application;
using Syllanote.Desktop.ViewModels;
using Syllanote.Infrastructure;
using Syllanote.Infrastructure.Backups;
using Syllanote.Infrastructure.Persistence;
using System;
using System.Threading.Tasks;

namespace Syllanote.Desktop
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Microsoft.UI.Xaml.Application
    {
        private Window? _window;
        private readonly string _databasePath;

        public IServiceProvider Services { get; }
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            var services = new ServiceCollection();

            services.AddApplication();

            services.AddTransient<NotebookViewModel>();
            services.AddTransient<MainWindow>();

            var localFolder =
                Windows.Storage.ApplicationData.Current.LocalFolder.Path;

            _databasePath =
                System.IO.Path.Combine(localFolder, "syllanote.db");

            services.AddInfrastructure($"Data Source={_databasePath}");

            Services = services.BuildServiceProvider();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(
            Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                await ApplyPendingRestoreAsync();
                await MigrateDatabaseAsync();
            }
            catch (Exception exception)
            {
                _window = new StartupErrorWindow(_databasePath, exception);
                _window.Activate();
                return;
            }

            _window = Services.GetRequiredService<MainWindow>();
            _window.Activate();
        }

        private async Task ApplyPendingRestoreAsync()
        {
            using var scope = Services.CreateScope();

            var restoreService = scope.ServiceProvider
                .GetRequiredService<PendingDatabaseRestoreService>();

            await restoreService.ApplyAsync();
        }

        private async Task MigrateDatabaseAsync()
        {
            using var scope = Services.CreateScope();

            var migrationService = scope.ServiceProvider
                .GetRequiredService<DatabaseMigrationService>();

            await migrationService.MigrateAsync();
        }
    }
}
