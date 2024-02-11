using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Extensions.Hosting;

using Wpf.Ui;

using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.Services;

public class ScripterLifetime : IHostLifetime
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly Application _application;
    private readonly Window _window;
    private readonly IUiContext _uiContext;
    private readonly SettingsViewManager _settings;

    public ScripterLifetime(
        IHostApplicationLifetime lifetime,
        Application application,
        Window window,
        IUiContext uiContext,
        SettingsViewManager settings)
    {
        _lifetime = lifetime;
        _application = application;
        _window = window;
        _uiContext = uiContext;
        _settings = settings;

        _application.Exit += OnApplicationExit;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        _uiContext.Invoke(() =>
        {
            _application.MainWindow = _window;
            _application.MainWindow.Show();

            if (!_settings.DarkMode)
            {
                // Theme.Apply(ThemeType.Light, updateAccent: false);
            }
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void OnApplicationExit(object sender, ExitEventArgs e)
    {
        _lifetime.StopApplication();
    }
}
