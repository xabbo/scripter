using System;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Hosting;

using GalaSoft.MvvmLight;

using Xabbo.Messages;
using Xabbo.Interceptor;

using Xabbo.Scripter.Scripting;
using Xabbo.Core.GameData;

namespace Xabbo.Scripter.Services
{
    public class ScriptHost : ObservableObject, IScriptHost, IJsonSerializer
    {
        private readonly JsonSerializerOptions
            _jsonSerializerOptions,
            _jsonSerializerOptionsIndented;

        private static readonly Random _rngGlobal = new();
        [ThreadStatic]
        private static Random? _rngLocal;
        private static Random CreateRng()
        {
            int seed;
            lock (_rngGlobal) seed = _rngGlobal.Next();
            return new Random(seed);
        }

        private readonly IHostApplicationLifetime _lifetime;
        private CancellationTokenSource? _globalCts;

        private bool _canExecute;
        public bool CanExecute
        {
            get => _canExecute;
            set => Set(ref _canExecute, value);
        }

        public IUiContext UiContext { get; private set; }

        public IGameDataManager GameDataManager { get; private set; }

        public IGameManager GameManager { get; private set; }

        public IMessageManager MessageManager { get; private set; }

        public IInterceptor Interceptor { get; private set; }

        public GlobalVariables GlobalVariables { get; private set; }

        IJsonSerializer IScriptHost.JsonSerializer => this;

        public CancellationToken CancellationToken => _globalCts?.Token ?? CancellationToken.None;

        public Random Random => (_rngLocal ??= CreateRng());

        public IObjectFormatter ObjectFormatter { get; } = new ObjectFormatter();

        public ScriptHost(
            IHostApplicationLifetime lifetime,
            IUiContext uiContext,
            IMessageManager messageManager,
            IInterceptor interceptor,
            IGameDataManager gameDataManager,
            IGameManager gameManager)
        {
            _jsonSerializerOptions = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            _jsonSerializerOptionsIndented = new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            };

            _lifetime = lifetime;

            UiContext = uiContext;
            MessageManager = messageManager;
            Interceptor = interceptor;
            GameDataManager = gameDataManager;
            GameManager = gameManager;

            GlobalVariables = new GlobalVariables();

            RefreshCancellationToken();

            interceptor.Connected += Interceptor_Connected;
            interceptor.Disconnected += Interceptor_Disconnected;
        }

        private void RefreshCancellationToken()
        {
            _globalCts?.Cancel();
            _globalCts?.Dispose();
            _globalCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping);
        }

        private void Interceptor_Connected(object? sender, GameConnectedEventArgs e)
        {
            CanExecute = true;
        }

        private void Interceptor_Disconnected(object? sender, EventArgs e)
        {
            CanExecute = false;

            RefreshCancellationToken();
        }

        public string Serialize<TValue>(TValue? value, bool indented = true)
            => JsonSerializer.Serialize(value, indented ? _jsonSerializerOptionsIndented : _jsonSerializerOptions);

        public T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);
    }
}
