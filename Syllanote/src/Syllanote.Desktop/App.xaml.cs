using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Syllanote.Application;
using Syllanote.Desktop.ViewModels;
using Syllanote.Infrastructure;
using Syllanote.Infrastructure.Persistence;
using System;
using Syllanote.Desktop.ViewModels;

namespace Syllanote.Desktop
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Microsoft.UI.Xaml.Application
    {
        private Window? _window;

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

            var databasePath =
                System.IO.Path.Combine(localFolder, "syllanote.db");

            services.AddInfrastructure($"Data Source={databasePath}");

            Services = services.BuildServiceProvider();

            using var scope = Services.CreateScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<SyllanoteDbContext>();

            dbContext.Database.Migrate();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(
            Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = Services.GetRequiredService<MainWindow>();
            _window.Activate();
        }
    }
}
