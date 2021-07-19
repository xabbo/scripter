using System;
using Xabbo.Core.GameData;

namespace Xabbo.Scripter.Events
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
