using System;

namespace Xabbo.Scripter.Scripting;

public enum ScriptStatus
{
    None,
    // Errors
    UnknownError,
    FileNotFound,
    CompileError,
    RuntimeError,
    TimedOut,
    Aborted,
    // Warnings

    // State
    Compiling,
    Running,
    Cancelling,
    Canceled,
    Complete
}
