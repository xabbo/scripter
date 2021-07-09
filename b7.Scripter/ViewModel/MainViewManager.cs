using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using GalaSoft.MvvmLight;

using Xabbo.Interceptor;

using b7.Scripter.Engine;
using b7.Scripter.Services;

namespace b7.Scripter.ViewModel
{
    public class MainViewManager : ObservableObject
    {
        private readonly ILogger _logger;

        private readonly ScriptEngine _scriptEngine;
        private readonly IGameDataManager _gameDataManager;

        private readonly IRemoteInterceptor _interceptor;

        private string _title = "b7 scripter";
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        public LogViewManager Log { get; }
        public ScriptsViewManager Scripts { get; }
        public AboutViewManager About { get; }
        public StatusBarViewManager StatusBar { get; }

        public MainViewManager(
            ILogger<MainViewManager> logger,
            ScriptEngine scriptEngine,
            IGameDataManager gameDataManager,
            IRemoteInterceptor interceptor,
            LogViewManager log,
            ScriptsViewManager scripts,
            AboutViewManager about,
            StatusBarViewManager statusBar)
        {
            Title = $"b7 scripter v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? " unknown"}";
#if DEBUG
            Title += " [DEBUG]";
#endif

            _logger = logger;

            _scriptEngine = scriptEngine;
            _gameDataManager = gameDataManager;

            _interceptor = interceptor;
            _interceptor.InterceptorConnected += OnInterceptorConnected;
            _interceptor.Connected += OnConnected;
            _interceptor.Disconnected += OnDisconnected;
            _interceptor.InterceptorDisconnected += OnInterceptorDisconnected;

            Log = log;
            Scripts = scripts;
            About = about;
            StatusBar = statusBar;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() => _scriptEngine.Initialize(), cancellationToken);
                await _gameDataManager.UpdateAsync();

                _logger.LogInformation($"b7 scripter initialized.");

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
            _logger.LogInformation("Game connection established.");
            _logger.LogInformation($"Client: {e.ClientType}");
            _logger.LogInformation($"Version: {e.ClientVersion}");
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
