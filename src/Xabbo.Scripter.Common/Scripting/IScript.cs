using System;
using System.Threading;

using Xabbo.Scripter.Model;

namespace Xabbo.Scripter.Scripting;

public interface IScript
{
    /// <summary>
    /// Gets the model.
    /// </summary>
    public ScriptModel Model { get; }

    /// <summary>
    /// Gets or sets the name of the script.
    /// </summary>
    string Name { get; set; }
    /// <summary>
    /// Gets or sets the group of the script.
    /// </summary>
    string Group { get; set; }
    /// <summary>
    /// Gets or sets the file path of the script.
    /// </summary>
    string FileName { get; set; }
    /// <summary>
    /// Gets or sets the code of the script.
    /// </summary>
    string Code { get; set; }
    /// <summary>
    /// Gets or sets whether the script has been modified or not.
    /// </summary>
    bool IsModified { get; set; }
    /// <summary>
    /// Gets or sets whether the script is compiling or not.
    /// </summary>
    bool IsCompiling { get; set; }
    /// <summary>
    /// Gets or sets whether the script is running or not.
    /// </summary>
    bool IsRunning { get; set; }
    /// <summary>
    /// Gets or sets the status of the script.
    /// </summary>
    ScriptStatus Status { get; set; }
    Exception? Error { get; set; }
    string? ErrorText { get; set; }
    DateTime? StartTime { get; set; }
    DateTime? EndTime { get; set; }
    TimeSpan? Runtime { get; }
    
    CancellationToken CancellationToken { get; }
    IProgress<ScriptUpdate>? Progress { get; }
}