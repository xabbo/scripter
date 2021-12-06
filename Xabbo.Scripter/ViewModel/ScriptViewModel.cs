using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.CodeAnalysis.Scripting;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using Xabbo.Scripter.Model;
using Xabbo.Scripter.Scripting;
using Xabbo.Scripter.Engine;
using Xabbo.Scripter.Services;
using Xabbo.Scripter.Events;
using Xabbo.Scripter.Util;

namespace Xabbo.Scripter.ViewModel
{
    public class ScriptViewModel : ObservableObject, IScript, IDisposable
    {
        private static readonly string EmptyHash = StringUtil.ComputeHash(string.Empty);

        private bool _disposed;

        private int _lastSaveLength;
        private string _lastSaveHash = EmptyHash;

        public ScriptModel Model { get; }
        public IScriptHost Host => Engine.Host;
        public ScriptEngine Engine { get; }

        public string Header => $"{Name}{(IsModified ? "*" : "")}";

        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Model.Name))
                {
                    return Path.GetFileNameWithoutExtension(Model.FileName);
                }
                else
                {
                    return Model.Name;
                }
            }

            set
            {
                if (Model.Name == value) return;
                Model.Name = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Header));
            }
        }

        public string Group
        {
            get => Model.GroupName;
            set
            {
                if (Model.GroupName == value) return;
                Model.GroupName = value;
                RaisePropertyChanged();
            }
        }

        public string FileName
        {
            get => Model.FileName;
            set
            {
                if (Model.FileName == value) return;
                Model.FileName = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(Header));
            }
        }

        private StringBuilder _code;
        public string Code
        {
            get => _code.ToString();
            set
            {
                _code.Clear();
                _code.Append(value);
                RaisePropertyChanged();

                IsModified =
                    value.Length != _lastSaveLength ||
                    StringUtil.ComputeHash(value) != _lastSaveHash;

                Match match = ScriptEngine.NameRegex.Match(value);
                if (match.Success)
                {
                    Name = match.Groups["name"].Value;
                }
                else
                {
                    Name = string.Empty;
                }

                match = ScriptEngine.GroupRegex.Match(value);
                if (match.Success)
                {
                    Group = match.Groups["group"].Value;
                }
                else
                {
                    Group = string.Empty;
                }
            }
        }

        private bool _isSavedToDisk;
        public bool IsSavedToDisk
        {
            get => _isSavedToDisk;
            set => Set(ref _isSavedToDisk, value);
        }

        private bool _isLoaded;
        public bool IsLoaded
        {
            get => _isLoaded;
            set => Set(ref _isLoaded, value);
        }

        private bool _isModified;
        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (Set(ref _isModified, value))
                    RaisePropertyChanged(nameof(Header));
            }
        }

        private bool _isCompiling;
        public bool IsCompiling
        {
            get => _isCompiling;
            set
            {
                if (Set(ref _isCompiling, value))
                    RaisePropertyChanged(nameof(IsWorking));
            }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (Set(ref _isRunning, value))
                    RaisePropertyChanged(nameof(IsWorking));
            }
        }

        public bool IsWorking => IsCompiling || IsRunning;

        private bool _isFaulted;
        public bool IsFaulted
        {
            get => _isFaulted;
            set => Set(ref _isFaulted, value);
        }

        private ScriptStatus _status;
        public ScriptStatus Status
        {
            get => _status;
            set
            {
                if (Set(ref _status, value))
                {
                    RaisePropertyChanged(nameof(IsRed));
                    RaisePropertyChanged(nameof(IsYellow));
                    RaisePropertyChanged(nameof(IsGreen));
                    RaisePropertyChanged(nameof(StatusText));
                }
            }
        }

        public bool IsRed => Status switch
        {
            ScriptStatus.FileNotFound or
            ScriptStatus.CompileError or
            ScriptStatus.RuntimeError or
            ScriptStatus.TimedOut or
            ScriptStatus.Aborted => true,
            _ => false
        };

        public bool IsYellow => Status switch
        {
            ScriptStatus.Compiling or 
            ScriptStatus.Cancelling => true,
            _ => false
        };

        public bool IsGreen => Status switch
        {
            ScriptStatus.Running => true,
            _ => false
        };

        public string StatusText => _status switch
        {
            ScriptStatus.UnknownError => "unknown error",
            ScriptStatus.FileNotFound => "file not found",
            ScriptStatus.CompileError => $"compile error{(ErrorText is null ? "" : ":")}{ErrorText}",
            ScriptStatus.RuntimeError => $"runtime error{(ErrorText is null ? "" : ":")}{ErrorText}",
            ScriptStatus.TimedOut => "timed out",
            ScriptStatus.Aborted => "aborted",
            ScriptStatus.Compiling => "compiling...",
            ScriptStatus.Running => "running...",
            ScriptStatus.Cancelling => "cancelling...",
            ScriptStatus.Canceled => "canceled",
            ScriptStatus.Complete => "complete",
            _ => "-",
        };

        private Exception? _error;
        public Exception? Error
        {
            get => _error;
            set => Set(ref _error, value);
        }

        private string? _errorText;
        public string? ErrorText
        {
            get => _errorText;
            set
            {
                if (Set(ref _errorText, value))
                    RaisePropertyChanged(nameof(StatusText));
            }
        }

        private DateTime? _startTime;
        public DateTime? StartTime
        {
            get => _startTime;
            set => Set(ref _startTime, value);
        }

        private DateTime? _endTime;
        public DateTime? EndTime
        {
            get => _endTime;
            set => Set(ref _endTime, value);
        }

        public TimeSpan? Runtime => (EndTime ?? DateTime.Now) - StartTime;

        public ScriptRunner<object>? Runner { get; set; }

        public ICommand ExecuteCommand { get; }
        public ICommand CancelCommand { get; }

        private CancellationTokenSource? _cts;
        public CancellationToken CancellationToken => _cts?.Token ?? CancellationToken.None;

        private bool _isCancelling;
        public bool IsCancelling
        {
            get => _isCancelling;
            set => Set(ref _isCancelling, value);
        }

        public IProgress<ScriptUpdate>? Progress { get; private set; }

        private bool _isLogging;
        public bool IsLogging
        {
            get => _isLogging;
            set => Set(ref _isLogging, value);
        }

        private StringBuilder _resultText = new();
        public string ResultText
        {
            get => _resultText.ToString();
            set
            {
                if (_resultText.Equals(value)) return;
                _resultText = new StringBuilder(value);
                RaisePropertyChanged();
            }
        }

        public event EventHandler<CompileErrorEventArgs>? CompileError;
        public event EventHandler<RuntimeErrorEventArgs>? RuntimeError;

        public ScriptViewModel(ScriptEngine engine, ScriptModel scriptModel)
        {
            Engine = engine;
            Model = scriptModel;

            _code = new StringBuilder();

            ExecuteCommand = new RelayCommand(OnExecute);
            CancelCommand = new RelayCommand(OnCancel);

            Progress = new Progress<ScriptUpdate>(OnProgress);
        }

        public void UpdateStatus(string status) => Progress?.Report(new ScriptUpdate(ScriptUpdateType.Status, status));
        public void LogMessage(string message) => Progress?.Report(new ScriptUpdate(ScriptUpdateType.Log, message));

        private void OnProgress(ScriptUpdate update)
        {
            if (update.UpdateType == ScriptUpdateType.Status)
            {
                IsLogging = false;

                _resultText.Clear();
                _resultText.Append(update.Message);
            }
            else
            {
                if (!IsLogging)
                {
                    _resultText.Clear();
                }

                IsLogging = true;

                if (_resultText.Length > 0)
                    _resultText.Append('\n');
                _resultText.Append(update.Message);
            }

            RaisePropertyChanged(nameof(ResultText));
        }

        public void RenewCancellationToken()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        private void OnCancel()
        {
            if ((!IsCompiling && !IsRunning) || IsCancelling) return;

            IsCancelling = true;
            _cts?.Cancel();
        }

        private void OnExecute()
        {
            Task.Run(() => Engine.Run(this));
        }

        private void UpdateHash()
        {
            _lastSaveLength = Code.Length;
            _lastSaveHash = StringUtil.ComputeHash(Code);
            IsModified = false;
        }

        public void Save()
        {
            if (!IsLoaded)
                throw new InvalidOperationException("Cannot save to disk; file is not loaded.");

            string filePath = Path.Combine(Engine.ScriptDirectory, FileName);

            UpdateHash();
            File.WriteAllText(filePath, Code);
            IsSavedToDisk = true;
        }

        public void Load()
        {
            string filePath = Path.Combine(Engine.ScriptDirectory, FileName);

            Code = File.ReadAllText(filePath);
            UpdateHash();

            IsLoaded = true;
        }

        public void UpdateRuntime()
        {
            RaisePropertyChanged(nameof(Runtime));
        }

        public void RaiseCompileError(CompilationErrorException error)
        {
            CompileError?.Invoke(this, new CompileErrorEventArgs(error));
        }

        public void RaiseRuntimeError(Exception error)
        {
            RuntimeError?.Invoke(this, new RuntimeErrorEventArgs(error));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {

            }
        }
    }
}
