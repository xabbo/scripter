using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using GalaSoft.MvvmLight;

namespace Xabbo.Scripter.Services;

public class ObservableLoggerProvider : ObservableObject, ILoggerProvider
{
    private StringBuilder _text = new();
    public string Text
    {
        get => _text.ToString();
        set
        {
            _text = new StringBuilder(value);
            RaisePropertyChanged();
        }
    }

    public ObservableLoggerProvider() { }

    public ILogger CreateLogger(string categoryName)
    {
        return new Logger(this, categoryName);
    }

    private void Update() => RaisePropertyChanged(nameof(Text));

    public void Log(string? message)
    {
        _text.Append(message);
        Update();
    }

    public void Clear()
    {
        _text.Clear();
        Update();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    class Logger : ILogger
    {
        private readonly ObservableLoggerProvider _provider;
        private readonly string _categoryName;

        public Logger(ObservableLoggerProvider provider, string categoryName)
        {
            _provider = provider;

            int index = categoryName.LastIndexOf('.');
            if (index > 0)
                categoryName = categoryName[(index + 1)..];

            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string logLevelText = logLevel switch
            {
                LogLevel.Trace => "trace",
                LogLevel.Debug => "debug",
                LogLevel.Information => "info",
                LogLevel.Warning => "!",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "CRITICAL",
                _ => "?"
            };

            _provider.Log($"[{DateTime.Now:HH:mm:ss}] [{logLevelText}] {formatter(state, exception)}\n");
        }
    }
}
