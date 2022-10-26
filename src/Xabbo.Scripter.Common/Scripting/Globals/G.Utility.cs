using System;
using System.ComponentModel;
using System.Threading.Tasks;

using Xabbo.Scripter.Runtime;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
    /// <summary>
    /// Gets if the script should run. Returns <see langword="false"/> if the script has finished/been cancelled.
    /// This is an alias for <c>!Ct.IsCancellationRequested</c>.
    /// </summary>
    public bool Run => !Ct.IsCancellationRequested;

    /// <summary>
    /// Delays the script for the specified duration.
    /// </summary>
    /// <param name="millisecondsDelay">The duration in milliseconds of the delay.</param>
    public void Delay(int millisecondsDelay) => Task.Delay(millisecondsDelay, Ct).GetAwaiter().GetResult();

    /// <summary>
    /// Delays the script for the specified duration.
    /// </summary>
    /// <param name="delay">The duration of the delay.</param>
    public void Delay(TimeSpan delay) => Task.Delay(delay, Ct).GetAwaiter().GetResult();

    /// <summary>
    /// Delays the script asynchronously for the specified duration.
    /// </summary>
    /// <param name="millisecondsDelay">The duration in milliseconds of the delay.</param>
    public Task DelayAsync(int millisecondsDelay) => Task.Delay(millisecondsDelay, Ct);

    /// <summary>
    /// Delays the script asynchronously for the specified duration.
    /// </summary>
    /// <param name="delay">The duration of the delay.</param>
    public Task DelayAsync(TimeSpan delay) => Task.Delay(delay, Ct);

    /// <summary>
    /// Pauses execution and keeps the script alive until it is cancelled or aborted.
    /// </summary>
    public void Wait() => Delay(-1);

    /// <summary>
    /// Returns a new <see cref="ScriptException"/> with the specified message
    /// which will be displayed in the log when thrown.
    /// </summary>
    public ScriptException Error(string message) => new ScriptException(message);

    /// <summary>
    /// Serializes an object to JSON.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="indented">Specifies whether to use indented formatting or not.</param>
    public string ToJson(object? value, bool indented = true) => _scriptHost.JsonSerializer.Serialize(value, indented);

    /// <summary>
    /// Deserializes an object from JSON.
    /// </summary>
    /// <typeparam name="TValue">The type of the object to deserialize.</typeparam>
    /// <param name="json">The JSON string that represents the object to deserialize.</param>
    public TValue? FromJson<TValue>(string json) => _scriptHost.JsonSerializer.Deserialize<TValue>(json);

    /// <summary>
    /// Sets script's status.
    /// </summary>
    public void Status(string? message) => _script.Progress?.Report(new ScriptUpdate(ScriptUpdateType.Status, message));

    /// <summary>
    /// Sets the script's status using <see cref="object.ToString"/>.
    /// </summary>
    public void Status(object? value) => Status(_scriptHost.ObjectFormatter.FormatObject(value));

    /// <summary>
    /// Logs the specified message to the script's output.
    /// </summary>
    public void Log(string message) => _script.Progress?.Report(new ScriptUpdate(ScriptUpdateType.Log, message));

    /// <summary>
    /// Logs the specified object to the script's output.
    /// </summary>
    public void Log(object? o) => Log(_scriptHost.ObjectFormatter.FormatObject(o));

    /// <summary>
    /// Logs an empty line to the script's output.
    /// </summary>
    public void Log() => Log(string.Empty);

    /// <summary>
    /// Cancels the script and sets <see cref="IsFinished"/> to <see langword="true"/>.
    /// Can be used to end the script from another task such as an intercept or event callback.
    /// </summary>
    public void Finish()
    {
        if (!IsFinished)
        {
            IsFinished = true;
            _cts.Cancel();
        }

        Ct.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Initializes a global variable if it does not yet exist.
    /// Returns <see langword="true"/> if the global variable was created.
    /// </summary>
    public bool InitGlobal(string variableName, dynamic value)
    {
        return _scriptHost.GlobalVariables.Init(variableName, value);
    }

    /// <summary>
    /// Initializes a global variable using a factory if it does not yet exist.
    /// Returns <see langword="true"/> if the global variable was created.
    /// </summary>
    public bool InitGlobal(string variableName, Func<dynamic> valueFactory)
    {
        return _scriptHost.GlobalVariables.Init(variableName, valueFactory);
    }

    /// <summary>
    /// Calculates the distance between two points.
    /// </summary>
    public static double Dist((int X, int Y) a, (int X, int Y) b)
    {
        return Math.Sqrt(
            Math.Pow(a.X - b.X, 2)
            + Math.Pow(a.Y - b.Y, 2)
        );
    }

    /// <summary>
    /// Queues the specified work to run on the thread pool.
    /// You must ensure that the task finishes once the script has completed 
    /// (when <see cref="Run"/> evaluates to false), otherwise the task may
    /// continue executing after the script has completed or has been cancelled.
    /// Calls to <c>Delay</c> are exit points for a task as an <see cref="OperationCanceledException"/>
    /// will be thrown when the script should no longer execute.
    /// </summary>
    public void RunTask(Action action) => Task.Run(action);

    /// <summary>
    /// Invokes the specified function on the UI context and returns the result.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public T InvokeOnUiThread<T>(Func<T> func) => _scriptHost.UiContext.Invoke(func);
}
