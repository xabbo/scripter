using System;

namespace b7.Scripter.Runtime
{
    public class ScriptException : Exception
    {
        public ScriptException(string message)
            : base(message)
        { }
    }
}
