using System;

namespace Xabbo.Scripter.Scripting
{
    public class ScriptUpdate
    {
        public ScriptUpdateType UpdateType { get; }
        public string? Message { get; }

        public ScriptUpdate(ScriptUpdateType updateType, string? message)
        {
            UpdateType = updateType;
            Message = message;
        }
    }

    public enum ScriptUpdateType { Status, Log }
}
