using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Extensions.Logging;

using GalaSoft.MvvmLight;

using MaterialDesignThemes.Wpf;

using Xabbo.Interceptor;
using Xabbo.Core.GameData;
using Xabbo.Scripter.Engine;
using Xabbo.Core;

namespace Xabbo.Scripter.ViewModel
{
    public class MainViewManager : ObservableObject
    {
        private readonly ILogger _logger;

        private readonly ScriptEngine _scriptEngine;
        private readonly IGameDataManager _gameDataManager;

        private readonly IRemoteInterceptor _interceptor;

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

        public MainViewManager(
            ILogger<MainViewManager> logger,
            ISnackbarMessageQueue snackbarMessageQueue,
            ScriptEngine scriptEngine,
            IGameDataManager gameDataManager,
            IRemoteInterceptor interceptor,
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

            _interceptor = interceptor;
            _interceptor.InterceptorConnected += OnInterceptorConnected;
            _interceptor.Connected += OnConnected;
            _interceptor.Disconnected += OnDisconnected;
            _interceptor.InterceptorDisconnected += OnInterceptorDisconnected;

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
                await _gameDataManager.LoadAsync(Hotel.FromIdentifier("us"), cancellationToken);

                _logger.LogInformation($"xabbo scripter initialized.");

                _ = _interceptor.RunAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"Initialization failed: {ex.Message}");
            }
        }

        private void OnInterceptorConnected(object? sender, EventArgs e)
        {
            _logger.LogInformation("Connection to G-Earth established.");
        }

        private void OnConnected(object? sender, GameConnectedEventArgs e)
        {
            _logger.LogInformation($"Game connection established. {e.ClientType} / {e.ClientVersion}");
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            _logger.LogWarning("Game connection lost.");
        }

        private void OnInterceptorDisconnected(object? sender, EventArgs e)
        {
            _logger.LogWarning("Connection to G-Earth lost.");
        }
    }
}
