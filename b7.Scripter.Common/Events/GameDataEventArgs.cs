using System;
using Xabbo.Core.GameData;

namespace b7.Scripter.Events
{
    public class GameDataEventArgs : EventArgs
    {
        public GameDataType Type { get; }

        public GameDataEventArgs(GameDataType type)
        {
            Type = type;
        }
    }
}
