using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Extensions.Hosting;

namespace b7.Scripter.Services
{
    public class WpfLifetime : IHostLifetime
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly Application _application;
        private readonly Window _window;

        public WpfLifetime(IHostApplicationLifetime lifetime, Application application, Window window)
        {
            _lifetime = lifetime;
            _application = application;
            _window = window;

            _application.Exit += OnApplicationExit;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _application.Shutdown();

            return Task.CompletedTask;
        }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            _application.MainWindow = _window;
            _application.MainWindow.Show();

            return Task.CompletedTask;
        }

        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            _lifetime.StopApplication();
        }
    }
}
