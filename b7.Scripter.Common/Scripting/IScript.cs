using System;
using System.Threading;

using b7.Scripter.Model;

namespace b7.Scripter.Scripting
{
    public interface IScript
    {
        public ScriptModel Model { get; }

        string Name { get; set; }
        string Group { get; set; }
        string Path { get; set; }
        string Code { get; set; }
        bool IsModified { get; set; }
        bool IsCompiling { get; set; }
        bool IsRunning { get; set; }
        ScriptStatus Status { get; set; }
        Exception? Error { get; set; }
        string? ErrorText { get; set; }
        DateTime? StartTime { get; set; }
        DateTime? EndTime { get; set; }
        TimeSpan? Runtime { get; }
        
        CancellationToken CancellationToken { get; }
        IProgress<ScriptUpdate>? Progress { get; }
    }
}