using System;
using Xabbo.Core.GameData;

namespace b7.Scripter.Events
{
    public class GameDataErrorEventArgs : GameDataEventArgs
    {
        public Exception Error { get; }

        public GameDataErrorEventArgs(GameDataType type, Exception error)
            : base(type)
        {
            Error = error;
        }
    }
}
