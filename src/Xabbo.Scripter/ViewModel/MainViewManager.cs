using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Extensions.Logging;

using GalaSoft.MvvmLight;

using MaterialDesignThemes.Wpf;

using Xabbo.Interceptor;
using Xabbo.Extension;

using Xabbo.Core;
using Xabbo.Core.GameData;

using Xabbo.Scripter.Engine;

namespace Xabbo.Scripter.ViewModel;

public class MainViewManager : ObservableObject
{
    private readonly ILogger _logger;

    private readonly ScriptEngine _scriptEngine;
    private readonly IGameDataManager _gameDataManager;

    private readonly IRemoteExtension _extension;

    private string _title = "xabbo scripter";
    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    public ISnackbarMessageQueue SnackbarMessageQueue { get; }

    public LogViewManager Log { get; }
    public ScriptsViewManager Scripts { get; }
    public ToolsViewManager Tools { get; }
    public AboutViewManager About { get; }
    public StatusBarViewManager StatusBar { get; }

    private GridLength _logHeight = GridLength.Auto;
    public GridLength LogHeight
    {
        get => _logHeight;
        set
        {
            Set(ref _logHeight, value);
        }
    }

    private bool _isPaneOpen = false;
    public bool IsPaneOpen
    {
        get => _isPaneOpen;
        set => Set(ref _isPaneOpen, value);
    }

    public MainViewManager(
        ILogger<MainViewManager> logger,
        ISnackbarMessageQueue snackbarMessageQueue,
        ScriptEngine scriptEngine,
        IGameDataManager gameDataManager,
        IRemoteExtension extension,
        LogViewManager log,
        ScriptsViewManager scripts,
        ToolsViewManager tools,
        AboutViewManager about,
        StatusBarViewManager statusBar)
    {
        string? version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (version is not null)
            Title += $" v{version}";
#if DEBUG
        Title += " [DEBUG]";
#endif

        _logger = logger;
        SnackbarMessageQueue = snackbarMessageQueue;

        _scriptEngine = scriptEngine;
        _gameDataManager = gameDataManager;

        _extension = extension;

        Log = log;
        Scripts = scripts;
        Tools = tools;
        About = about;
        StatusBar = statusBar;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => _scriptEngine.Initialize(), cancellationToken).ConfigureAwait(false);

            _logger.LogInformation($"xabbo scripter initialized.");

            _ = _extension.RunAsync(default);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Initialization failed: {message}", ex.Message);
        }
    }
}
