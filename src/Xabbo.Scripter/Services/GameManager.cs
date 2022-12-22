using System;
using Microsoft.Extensions.Logging;

using Xabbo.Messages;
using Xabbo.Extension;
using Xabbo.Core.Game;
using Xabbo.Core.GameData;

namespace Xabbo.Scripter.Services;

public class GameManager : IGameManager
{
    private readonly ILogger _logger;

    private readonly IMessageManager _messages;
    private readonly IRemoteExtension _extension;
    private readonly IGameDataManager _gameDataManager;

    public ProfileManager ProfileManager { get; private set; }
    public RoomManager RoomManager { get; private set; }
    public TradeManager TradeManager { get; private set; }
    public InventoryManager InventoryManager { get; private set; }
    public FriendManager FriendManager { get; private set; }

    public GameManager(
        ILogger<GameManager> logger,
        IMessageManager messages,
        IRemoteExtension extension,
        IGameDataManager gameDataManager,
        ProfileManager profileManager,
        FriendManager friendManager,
        RoomManager roomManager,
        InventoryManager inventoryManager,
        TradeManager tradeManager)
    {
        _logger = logger;

        _messages = messages;

        _extension = extension;
        _extension.InterceptorConnected += OnInterceptorConnected;
        _extension.Connected += Interceptor_ConnectionStart;
        _extension.Disconnected += Interceptor_ConnectionEnd;
        _extension.InterceptorDisconnected += Interceptor_Disconnected;

        _gameDataManager = gameDataManager;

        ProfileManager = profileManager;
        FriendManager = friendManager;
        RoomManager = roomManager;
        InventoryManager = inventoryManager;
        TradeManager = tradeManager;
    }

    private void OnInterceptorConnected(object? sender, EventArgs e)
    {
        _logger.LogInformation("Connection to G-Earth established.");
    }

    private async void Interceptor_ConnectionStart(object? sender, GameConnectedEventArgs e)
    {
        // Dispatcher = new MessageDispatcher(_messages.Headers);
        // Components = new ComponentManager(this);
        // Components.LoadComponents(XabboComponent.GetCoreComponentTypes());

        // TODO Scoped component loading

        _logger.LogInformation("Game connection established. {clientType} / {clientVersion}", e.ClientType, e.ClientVersion);

        try
        {
            var hotel = Hotel.FromGameHost(e.Host);

            _logger.LogInformation("Loading game data for hotel: {hotel}...", hotel.Name);

            await _gameDataManager.LoadAsync(Hotel.FromGameHost(e.Host), _extension.DisconnectToken);

            _logger.LogInformation("Game data loaded.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load game data: {message}", ex.Message);
        }
    }

    private void Interceptor_ConnectionEnd(object? sender, EventArgs e)
    {
        _logger.LogWarning("Game connection ended.");
    }

    private void Interceptor_Disconnected(object? sender, EventArgs e)
    {
        _logger.LogWarning("Lost connection to G-Earth.");
    }
}
