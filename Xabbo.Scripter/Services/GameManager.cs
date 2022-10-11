using System;
using System.Threading.Tasks;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.Core.Game;
using Xabbo.Extension;

namespace Xabbo.Scripter.Services;

public class GameManager : IGameManager
{
    private readonly IMessageManager _messages;
    private readonly IRemoteExtension _extension;

    public ValueTask SendAsync(IReadOnlyPacket packet) => _extension.SendAsync(packet);

    public event EventHandler? InitializeComponents;

    public ProfileManager ProfileManager { get; private set; }
    public RoomManager RoomManager { get; private set; }
    public TradeManager TradeManager { get; private set; }
    public InventoryManager InventoryManager { get; private set; }
    public FriendManager FriendManager { get; private set; }

    public GameManager(IMessageManager messages, IRemoteExtension extension,
        ProfileManager profileManager,
        FriendManager friendManager,
        RoomManager roomManager,
        InventoryManager inventoryManager,
        TradeManager tradeManager)
    {
        _messages = messages;

        _extension = extension;
        _extension.Connected += Interceptor_ConnectionStart;
        _extension.Disconnected += Interceptor_ConnectionEnd;
        _extension.Disconnected += Interceptor_Disconnected;

        ProfileManager = profileManager;
        FriendManager = friendManager;
        RoomManager = roomManager;
        InventoryManager = inventoryManager;
        TradeManager = tradeManager;
    }

    private void Interceptor_ConnectionStart(object? sender, EventArgs e)
    {
        // Dispatcher = new MessageDispatcher(_messages.Headers);
        // Components = new ComponentManager(this);
        // Components.LoadComponents(XabboComponent.GetCoreComponentTypes());

        // TODO Scoped component loading

        InitializeComponents?.Invoke(this, EventArgs.Empty);
    }

    private void Interceptor_ConnectionEnd(object? sender, EventArgs e)
    {

    }

    private void Interceptor_Disconnected(object? sender, EventArgs e)
    {

    }
}
