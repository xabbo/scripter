using System;

namespace Xabbo.Scripter.Events;

public class CompileErrorEventArgs : EventArgs
{
    public Exception Error { get; }

    public CompileErrorEventArgs(Exception error)
    {
        Error = error;
    }
}
