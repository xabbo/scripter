using System;

using Xabbo.Core;
using Xabbo.Core.Game;

namespace Xabbo.Scripter.Services
{
    public interface IGameManager
    {
        event EventHandler? InitializeComponents;
        
        ProfileManager ProfileManager { get; }
        FriendManager FriendManager { get; }
        RoomManager RoomManager { get; }
        TradeManager TradeManager { get; }
    }
}
