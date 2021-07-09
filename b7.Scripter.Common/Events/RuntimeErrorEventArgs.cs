using System;

namespace b7.Scripter.Events
{
    public class RuntimeErrorEventArgs : EventArgs
    {
        public Exception Error { get; }

        public RuntimeErrorEventArgs(Exception error)
        {
            Error = error;
        }
    }
}
