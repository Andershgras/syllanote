using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Syllanote.Infrastructure;
using Syllanote.Infrastructure.Persistence;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Syllanote.Desktop
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
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

            var localFolder =
                Windows.Storage.ApplicationData.Current.LocalFolder.Path;

            var databasePath =
                System.IO.Path.Combine(localFolder, "syllanote.db");

            services.AddInfrastructure($"Data Source={databasePath}");

            Services = services.BuildServiceProvider();

            using var scope = Services.CreateScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<SyllanoteDbContext>();

            dbContext.Database.EnsureCreated();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
