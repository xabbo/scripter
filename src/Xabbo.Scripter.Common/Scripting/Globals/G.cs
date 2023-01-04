using System;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;

using Xabbo.Messages;
using Xabbo.Messages.Dispatcher;
using Xabbo.Interceptor;
using Xabbo.Core.Game;
using Xabbo.Core.GameData;

using Xabbo.Scripter.Services;

namespace Xabbo.Scripter.Scripting;

/// <summary>
/// The xabbo scripter globals class.
/// Contains the methods and properties that are globally accessible from scripts.
/// </summary>
public partial class G : IDisposable
{
    private const int
        DEFAULT_TIMEOUT = 10000,
        DEFAULT_LONG_TIMEOUT = 30000; 

    private readonly IScriptHost _scriptHost;
    private readonly IScript _script;

    private readonly List<IDisposable> _disposables = new();
    private readonly List<Intercept> _intercepts = new();

    private readonly CancellationTokenSource _cts;

    private IMessageDispatcher _dispatcher => _scriptHost.Extension.Dispatcher;

    private ProfileManager _profileManager => _scriptHost.GameManager.ProfileManager;
    private FriendManager _friendManager => _scriptHost.GameManager.FriendManager;
    private RoomManager _roomManager => _scriptHost.GameManager.RoomManager;
    private InventoryManager _inventoryManager => _scriptHost.GameManager.InventoryManager;
    private TradeManager _tradeManager => _scriptHost.GameManager.TradeManager;

    /// <summary>
    /// Gets the interceptor service.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IInterceptor Interceptor => _scriptHost.Extension;

    /// <summary>
    /// Gets the current client type.
    /// </summary>
    public ClientType Client => Interceptor.Client;

    /// <summary>
    /// Gets the current client identifier.
    /// </summary>
    public string ClientIdentifier => Interceptor.ClientIdentifier;

    /// <summary>
    /// Gets the current client version.
    /// </summary>
    public string ClientVersion => Interceptor.ClientVersion;

    /// <summary>
    /// Gets the current hotel.
    /// </summary>
    public Hotel Hotel => Interceptor.Hotel;

    /// <summary>
    /// Gets the cancellation token which signals when the script has been
    /// cancelled or aborted, and execution should no longer continue.
    /// </summary>
    public CancellationToken Ct { get; }

    /// <summary>
    /// Gets if the script was terminated with <see cref="Finish"/>.
    /// </summary>
    public bool IsFinished { get; private set; }

    /// <summary>
    /// Gets the message headers.
    /// </summary>
    public IMessageManager Messages => _scriptHost.MessageManager;

    /// <summary>
    /// Gets the incoming message headers.
    /// </summary>
    public Incoming In => Messages.In;

    /// <summary>
    /// Gets the outgoing message headers.
    /// </summary>
    public Outgoing Out => Messages.Out;

    /// <summary>
    /// Gets the figure data.
    /// </summary>
    public FigureData FigureData => _scriptHost.GameDataManager.Figure ?? throw new Exception("Figure data is unavailable.");

    /// <summary>
    /// Gets the furni data.
    /// </summary>
    public FurniData FurniData => _scriptHost.GameDataManager.Furni ?? throw new Exception("Furni data is unavailable.");

    /// <summary>
    /// Gets the product data.
    /// </summary>
    public ProductData ProductData => _scriptHost.GameDataManager.Products ?? throw new Exception("Product data is unavailable.");

    /// <summary>
    /// Gets the external texts.
    /// </summary>
    public ExternalTexts Texts => _scriptHost.GameDataManager.Texts ?? throw new Exception("External texts are unavailable.");

    /// <summary>
    /// Gets the global variables of the scripter.
    /// </summary>
    public dynamic Global => _scriptHost.GlobalVariables;

    /// <summary>
    /// Constructs a new instance of the scripter globals.
    /// </summary>
    /// <param name="scriptHost">A reference to the script host.</param>
    /// <param name="script">A reference to the script.</param>
    public G(IScriptHost scriptHost, IScript script)
    {
        _scriptHost = scriptHost;
        _script = script;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(script.CancellationToken);
        Ct = _cts.Token;
    }

    /// <summary>
    /// Disposes of the globals class - cancels the script, deregisters intercept callbacks and unsubscribes from events.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        _cts.Cancel();
        _cts.Dispose();

        lock (_intercepts)
        {
            foreach (var intercept in _intercepts)
                _dispatcher.RemoveIntercept(intercept.Header, intercept.Callback);
        }

        lock (_disposables)
        {
            foreach (var unsubscriber in _disposables)
                unsubscriber.Dispose();
        }
    }

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
