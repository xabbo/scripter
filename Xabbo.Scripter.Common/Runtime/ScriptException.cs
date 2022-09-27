using System;

namespace Xabbo.Scripter.Runtime;

public class ScriptException : Exception
{
    public ScriptException(string message)
        : base(message)
    { }
}
