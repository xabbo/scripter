using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ComponentModel;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.Interceptor.Tasks;
using Xabbo.Interceptor.Dispatcher;
using Xabbo.Core;
using Xabbo.Core.Events;
using Xabbo.Core.Game;
using Xabbo.Core.GameData;
using Xabbo.Core.Tasks;

using Xabbo.Scripter.Runtime;
using Xabbo.Scripter.Services;
using Xabbo.Scripter.Tasks;

namespace Xabbo.Scripter.Scripting
{
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

        private IInterceptDispatcher _dispatcher => _scriptHost.Interceptor.Dispatcher;

        private ProfileManager _profileManager => _scriptHost.GameManager.ProfileManager;
        private FriendManager _friendManager => _scriptHost.GameManager.FriendManager;
        private RoomManager _roomManager => _scriptHost.GameManager.RoomManager;
        private InventoryManager _inventoryManager => _scriptHost.GameManager.InventoryManager;
        private TradeManager _tradeManager => _scriptHost.GameManager.TradeManager;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public IInterceptor Interceptor => _scriptHost.Interceptor;
        public ClientType ClientType => Interceptor.Client;

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
        public FigureData FigureData => _scriptHost.GameDataManager.FigureData ?? throw new Exception("Figure data is unavailable.");

        /// <summary>
        /// Gets the furni data.
        /// </summary>
        public FurniData FurniData => _scriptHost.GameDataManager.FurniData ?? throw new Exception("Furni data is unavailable.");

        /// <summary>
        /// Gets the product data.
        /// </summary>
        public ProductData ProductData => _scriptHost.GameDataManager.ProductData ?? throw new Exception("Product data is unavailable.");

        /// <summary>
        /// Gets the external texts.
        /// </summary>
        public ExternalTexts Texts => _scriptHost.GameDataManager.ExternalTexts ?? throw new Exception("External texts are unavailable.");

        /// <summary>
        /// Gets the global variables of the scripter.
        /// </summary>
        public dynamic Global => _scriptHost.GlobalVariables;

        public G(IScriptHost scriptHost, IScript script)
        {
            _scriptHost = scriptHost;
            _script = script;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(script.CancellationToken);
            Ct = _cts.Token;
        }

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

        #region - Utility -
        /// <summary>
        /// Gets whether the script should run or not. Returns <c>false</c> if the script has finished/been cancelled.
        /// This is an alias for <c>!Ct.IsCancellationRequested</c>.
        /// </summary>
        public bool Run => !Ct.IsCancellationRequested;

        /// <summary>
        /// Delays the script for the specified duration.
        /// </summary>
        /// <param name="millisecondsDelay">The duration in milliseconds of the delay.</param>
        public void Delay(int millisecondsDelay) => Task.Delay(millisecondsDelay, Ct).GetAwaiter().GetResult();

        /// <summary>
        /// Delays the script for the specified duration.
        /// </summary>
        /// <param name="delay">The duration of the delay.</param>
        public void Delay(TimeSpan delay) => Task.Delay(delay, Ct).GetAwaiter().GetResult();

        /// <summary>
        /// Delays the script asynchronously for the specified duration.
        /// </summary>
        /// <param name="millisecondsDelay">The duration in milliseconds of the delay.</param>
        public Task DelayAsync(int millisecondsDelay) => Task.Delay(millisecondsDelay, Ct);

        /// <summary>
        /// Delays the script asynchronously for the specified duration.
        /// </summary>
        /// <param name="delay">The duration of the delay.</param>
        public Task DelayAsync(TimeSpan delay) => Task.Delay(delay, Ct);

        /// <summary>
        /// Pauses execution and keeps the script alive until it is cancelled or aborted.
        /// </summary>
        public void Wait() => Delay(-1);

        /// <summary>
        /// Returns a new <see cref="ScriptException"/> with the specified message
        /// which will be displayed in the log when thrown.
        /// </summary>
        public ScriptException Error(string message) => new ScriptException(message);

        /// <summary>
        /// Serializes an object to JSON.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="indented">Specifies whether to use indented formatting or not.</param>
        public string ToJson(object? value, bool indented = true) => _scriptHost.JsonSerializer.Serialize(value, indented);

        /// <summary>
        /// Deserializes an object from JSON.
        /// </summary>
        /// <typeparam name="TValue">The type of the object to deserialize.</typeparam>
        /// <param name="json">The JSON string that represents the object to deserialize.</param>
        public TValue? FromJson<TValue>(string json) => _scriptHost.JsonSerializer.Deserialize<TValue>(json);

        /// <summary>
        /// Sets script's status.
        /// </summary>
        public void Status(string? message) => _script.Progress?.Report(new ScriptUpdate(ScriptUpdateType.Status, message));

        /// <summary>
        /// Sets the script's status using <see cref="object.ToString"/>.
        /// </summary>
        public void Status(object? value) => Status(value?.ToString() ?? "null");

        /// <summary>
        /// Logs the specified message to the script's output.
        /// </summary>
        public void Log(string message) => _script.Progress?.Report(new ScriptUpdate(ScriptUpdateType.Log, message));

        /// <summary>
        /// Logs the specified object to the script's output.
        /// </summary>
        public void Log(object? o) => Log(o?.ToString() ?? "null");

        /// <summary>
        /// Logs an empty line to the script's output.
        /// </summary>
        public void Log() => Log(string.Empty);

        /// <summary>
        /// Creates an awaitable task that asynchronously yields back to the current context when awaited.
        /// </summary>
        public YieldAwaitable Yield() => Task.Yield();

        /// <summary>
        /// Cancels the script and sets <see cref="IsFinished"/> to <c>true</c>.
        /// Can be used to complete the script from another task such as an intercept or event callback.
        /// </summary>
        public void Finish()
        {
            if (!IsFinished)
            {
                IsFinished = true;
                _cts.Cancel();
            }

            Ct.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Initializes a global variable if it does not yet exist.
        /// </summary>
        public bool InitGlobal(string variableName, dynamic value)
        {
            return _scriptHost.GlobalVariables.Init(variableName, value);
        }

        /// <summary>
        /// Initializes a global variable using a factory if it does not yet exist.
        /// </summary>
        public bool InitGlobal(string variableName, Func<dynamic> valueFactory)
        {
            return _scriptHost.GlobalVariables.Init(variableName, valueFactory);
        }

        /// <summary>
        /// Calculates the distance between two points.
        /// </summary>
        public static double Dist((int X, int Y) a, (int X, int Y) b)
        {
            return Math.Sqrt(
                Math.Pow(a.X - b.X, 2)
                + Math.Pow(a.Y - b.Y, 2)
            );
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool.
        /// You must ensure that the task finishes once the script has completed 
        /// (when <see cref="Run"/> evaluates to false), otherwise the task may
        /// continue executing after the script has completed or has been cancelled.
        /// Calls to <c>Delay</c> are exit points for a task as an <see cref="OperationCanceledException"/>
        /// will be thrown when the script should no longer execute.
        /// </summary>
        public void RunTask(Action action) => Task.Run(action);
        #endregion

        #region - Assertion -
        private static bool CheckDest(Destination destination, Header header)
        {
            return
                header.Destination == Destination.Unknown ||
                header.Destination == destination;
        }

        private bool CheckHeader(Header header) => header.GetClientHeader(ClientType) is not null;

        private void AssertTargetHeaders(Destination destination, Header[] headers)
        {
            if (headers == null || headers.Length == 0)
                throw new Exception("At least one target header must be specified.");

            var incorrectDest = headers.Where(header => !CheckDest(destination, header));
            if (incorrectDest.Any())
            {
                throw new Exception(
                    $"Invalid {destination.ToDirection().ToString().ToLower()} target headers: " +
                    string.Join(", ", incorrectDest) + "."
                );
            }

            var invalidHeaders = headers
                .Where(header => !CheckHeader(header))
                .Select(header => new Identifier(destination, header.GetName(ClientType) ?? ""))
                .ToArray();

            if (invalidHeaders.Any())
            {
                throw new Exception(
                    $"Unresolved/invalid target headers: " +
                    new Identifiers(invalidHeaders).ToString()
                );
            }
        }
        #endregion

        #region - Net -
        /// <summary>
        /// Constructs a packet with the specified header and values, then sends it to the server.
        /// </summary>
        /// <param name="header">The header of the message to send.</param>
        /// <param name="values">The values to write to the packet.</param>
        public void Send(Header header, params object[] values) => Send(Packet.Compose(Interceptor.Client, header, values));

        /// <summary>
        /// Sends the specified packet to the client or server.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void Send(IReadOnlyPacket packet) => Interceptor.Send(packet);

        /// <summary>
        /// Captures a packet with a header that matches any of the specified headers.
        /// </summary>
        /// <param name="headers">The message headers to listen for.</param>
        /// <param name="timeout">The time to wait for a packet to be captured.</param>
        /// <param name="block">Whether to block the captured packet.</param>
        /// <returns>The first packet with a header that matches one of the specified headers.</returns>
        public IReadOnlyPacket Receive(HeaderSet headers, int timeout = -1, bool block = false)
            => ReceiveAsync(headers, timeout, block).GetAwaiter().GetResult();

        public IReadOnlyPacket Receive(ITuple tuple, int timeout = -1, bool block = false)
        {
            return Receive(HeaderSet.FromTuple(tuple), timeout, block);
        }

        /// <summary>
        /// Asynchronously captures a packet with a header that matches any of specified headers.
        /// </summary>
        /// <param name="headers">The message headers to listen for.</param>
        /// <param name="timeout">The time to wait for a packet to be captured.</param>
        /// <param name="block">Whether to block the captured packet.</param>
        /// <returns>The first packet with a header that matches one of the specified headers.</returns>
        public Task<IPacket> ReceiveAsync(HeaderSet headers, int timeout = -1, bool block = false)
        {
            return new CaptureMessageTask(Interceptor, headers, block)
                .ExecuteAsync(timeout, Ct);
        }

        /// <summary>
        /// Attempts to capture a packet with a header that matches any of the specified headers.
        /// </summary>
        /// <param name="headers">The message headers to listen for.</param>
        /// <param name="timeout">The time to wait for a packet to be captured.</param>
        /// <param name="packet">The packet that was captured.</param>
        /// <param name="block">Whether to block the captured packet.</param>
        /// <returns>True if a packet was successfully captured, or false if the operation timed out.</returns>
        public bool TryReceive(HeaderSet headers, out IReadOnlyPacket? packet, int timeout = -1, bool block = false)
        {
            packet = null;
            try
            {
                packet = Receive(headers, timeout, block);
                return true;
            }
            catch (OperationCanceledException)
            when (!Ct.IsCancellationRequested)
            {
                return false;
            }
        }

        /// <summary>
        /// Registers a callback to be invoked when a packet with the specified header is intercepted.
        /// </summary>
        public void OnIntercept(Header header, Action<InterceptArgs> callback)
        {
            lock (_intercepts)
            {
                _dispatcher.AddIntercept(header, callback, ClientType);
                _intercepts.Add(new Intercept(Interceptor.Dispatcher, header, callback));
            }
        }

        /// <summary>
        /// Registers a callback to be invoked when a packet with any of the specified headers is intercepted.
        /// </summary>
        public void OnIntercept(ITuple headers, Action<InterceptArgs> callback) => OnIntercept(HeaderSet.FromTuple(headers), callback);
        
        /// <summary>
        /// Registers a callback to be invoked when a packet with any of the specified headers is intercepted.
        /// </summary>
        public void OnIntercept(HeaderSet headers, Action<InterceptArgs> callback)
        {
            foreach (Header header in headers)
            {
                OnIntercept(header, callback);
            }
        }

        /// <summary>
        /// Registers a callback to be invoked when a packet with the specified header is intercepted.
        /// </summary>
        public void OnIntercept(Header header, Func<InterceptArgs, Task> callback)
            => OnIntercept(header, e => { callback(e); });

        /// <summary>
        /// Registers a callback to be invoked when a packet with any of the specified headers is intercepted.
        /// </summary>
        public void OnIntercept(HeaderSet headers, Func<InterceptArgs, Task> callback)
           => OnIntercept(headers, e => { callback(e); });
        #endregion

        #region - Room -
        /// <summary>
        /// Gets the ID of the current room that the user is in, or <c>-1</c> if the user is not in a room.
        /// </summary>
        public long RoomId => _roomManager.CurrentRoomId;

        /// <summary>
        /// Gets if the user is ringing the doorbell.
        /// </summary>
        public bool IsRingingDoorbell => _roomManager.IsRingingDoorbell;

        /// <summary>
        /// Gets if the user is in the room queue.
        /// </summary>
        public bool IsInQueue => _roomManager.IsInQueue;

        /// <summary>
        /// Gets the current queue position the user is in.
        /// </summary>
        public int QueuePosition => _roomManager.QueuePosition;

        /// <summary>
        /// Gets if the user is currently loading a room.
        /// </summary>
        public bool IsLoadingRoom => _roomManager.IsLoadingRoom;

        /// <summary>
        /// Gets if the user is in a room.
        /// </summary>
        public bool IsInRoom => _roomManager.IsInRoom;

        /// <summary>
        /// Gets the current room.
        /// </summary>
        public IRoom? Room => _roomManager.Room;

        /// <summary>
        /// Gets the door tile of the room.
        /// </summary>
        public Tile? DoorTile => Room?.DoorTile;

        /// <summary>
        /// Gets the heightmap of the room.
        /// </summary>
        public IHeightmap? Heightmap => Room?.Heightmap;

        /// <summary>
        /// Gets the floor plan of the room.
        /// </summary>
        public IFloorPlan? FloorPlan => Room?.FloorPlan;
        #endregion

        #region - Room permissions -
        /// <summary>
        /// Gets if the user has permission to mute.
        /// </summary>
        public bool CanMute => _roomManager.CanMute;

        /// <summary>
        /// Gets if the user has permission to kick.
        /// </summary>
        public bool CanKick => _roomManager.CanKick;

        /// <summary>
        /// Gets if the user has permission to ban.
        /// </summary>
        public bool CanBan => _roomManager.CanBan;

        /// <summary>
        /// Gets if the user has permission to unban.
        /// This is an alias for <see cref="IsRoomOwner"/>.
        /// </summary>
        public bool CanUnban => IsRoomOwner;

        /// <summary>
        /// Gets if the user is the room owner.
        /// </summary>
        public bool IsRoomOwner => _roomManager.IsOwner;
        #endregion

        #region - Furni -
        /// <summary>
        /// Gets the furni in the room.
        /// </summary>
        public IEnumerable<IFurni> Furni => _roomManager.Room?.Furni ?? Enumerable.Empty<IFurni>();
        /// <summary>
        /// Gets the floor items in the room.
        /// </summary>
        public IEnumerable<IFloorItem> FloorItems => _roomManager.Room?.FloorItems ?? Enumerable.Empty<IFloorItem>();
        /// <summary>
        /// Gets the wall items in the room.
        /// </summary>
        public IEnumerable<IWallItem> WallItems => _roomManager.Room?.WallItems ?? Enumerable.Empty<IWallItem>();
        /// <summary>
        /// Gets the floor items with the specified ID.
        /// </summary>
        public IFloorItem? GetFloorItem(long id) => FloorItems.FirstOrDefault(item => item.Id == id);
        /// <summary>
        /// Gets the wall item with the specified ID.
        /// </summary>
        public IWallItem? GetWallItem(long id) => WallItems.FirstOrDefault(item => item.Id == id);
        #endregion

        #region - Entities -
        private T? GetEntity<T>(int index) where T : class, IEntity => _roomManager.Room?.GetEntity<T>(index);
        private T? GetEntity<T>(string name) where T : class, IEntity
            => _roomManager.Room?.GetEntity<T>(name);
        private T? GetEntityById<T>(long id) where T : class, IEntity => _roomManager.Room?.GetEntityById<T>(id);

        /// <summary>
        /// Gets the entities in the room.
        /// </summary>
        public IEnumerable<IEntity> Entities => _roomManager.Room?.Entities ?? Enumerable.Empty<IEntity>();
        /// <summary>
        /// Gets the users in the room.
        /// </summary>
        public IEnumerable<IRoomUser> Users => Entities.OfType<IRoomUser>();
        /// <summary>
        /// Gets the pets in the room.
        /// </summary>
        public IEnumerable<IPet> Pets => Entities.OfType<IPet>();
        /// <summary>
        /// Gets the bots in the room.
        /// </summary>
        public IEnumerable<IBot> Bots => Entities.OfType<IBot>();

        /// <summary>
        /// Gets the entity with the specified index.
        /// </summary>
        /// <param name="index">The index of the entity to get.</param>
        /// <returns>The entity with the specified index, or <c>null</c> if it doesn't exist.</returns>
        public IEntity? GetEntityByIndex(int index) => GetEntity<IEntity>(index);
        /// <summary>
        /// Gets the entity with the specified name.
        /// </summary>
        /// <param name="name">The name of the entity to get.</param>
        /// <returns>The entity with the specified name, or <c>null</c> if it doesn't exist.</returns>
        public IEntity? GetEntity(string name) => GetEntity<IEntity>(name);
        /// <summary>
        /// Gets the entity with the specified id.
        /// </summary>
        /// <param name="id">The id of the entity to get.</param>
        /// <returns>The entity with the specified id, or <c>null</c> if it doesn't exist.</returns>
        public IEntity? GetEntityById(long id) => GetEntityById<IEntity>(id);

        /// <summary>
        /// Gets the user with the specified index.
        /// </summary>
        /// <param name="index">The index of the user to get.</param>
        /// <returns>The user with the specified index, or <c>null</c> if it doesn't exist.</returns>
        public IRoomUser? GetUser(int index) => GetEntity<IRoomUser>(index);
        /// <summary>
        /// Gets the user with the specified name.
        /// </summary>
        /// <param name="name">The name of the user to get.</param>
        /// <returns>The user with the specified name, or <c>null</c> if it doesn't exist.</returns>
        public IRoomUser? GetUser(string name) => GetEntity<IRoomUser>(name);
        /// <summary>
        /// Gets the user with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the user to get.</param>
        /// <returns>The user with the specified ID, or <c>null</c> if it doesn't exist.</returns>
        public IRoomUser? GetUserById(long id) => GetEntityById<IRoomUser>(id);

        /// <summary>
        /// Gets the pet with the specified index.
        /// </summary>
        /// <param name="index">The index of the pet to get.</param>
        /// <returns>The pet with the specified index, or <c>null</c> if it doesn't exist.</returns>
        public IPet? GetPet(int index) => GetEntity<IPet>(index);
        /// <summary>
        /// Gets the pet with the specified name.
        /// </summary>
        /// <param name="name">The name of the pet to get.</param>
        /// <returns>The pet with the specified name, or <c>null</c> if it doesn't exist.</returns>
        public IPet? GetPet(string name) => GetEntity<IPet>(name);
        /// <summary>
        /// Gets the pet with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the pet to get.</param>
        /// <returns>The pet with the specified ID, or <c>null</c> if it doesn't exist.</returns>
        public IPet? GetPetById(long id) => GetEntityById<IPet>(id);

        /// <summary>
        /// Gets the bot with the specified index.
        /// </summary>
        /// <param name="index">The index of the bot to get.</param>
        /// <returns>The bot with the specified index, or <c>null</c> if it doesn't exist.</returns>
        public IBot? GetBot(int index) => GetEntity<IBot>(index);
        /// <summary>
        /// Gets the bot with the specified name.
        /// </summary>
        /// <param name="name">The name of the bot to get.</param>
        /// <returns>The bot with the specified name, or <c>null</c> if it doesn't exist.</returns>
        public IBot? GetBot(string name) => GetEntity<IBot>(name);
        /// <summary>
        /// Gets the bot with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the bot to get.</param>
        /// <returns>The bot with the specified ID, or <c>null</c> if it doesn't exist.</returns>
        public IBot? GetBotById(long id) => GetEntityById<IBot>(id);

        /// <summary>
        /// Gets the user's own <see cref="IRoomUser"/> instance.
        /// </summary>
        public IRoomUser? Self => GetUserById(UserId);
        #endregion

        #region - Client-side -
        public void Info(string message) => ShowBubble(message, 34);

        public void ShowBubble(string message, int bubble = 34, int index = -1)
        {
            if (index == -1) index = Self?.Index ?? -1;

            Send(In.Whisper, index, message, 0, bubble, 0, 0);
        }
        #endregion

        #region - User profile -
        /// <summary>
        /// Gets the user's data.
        /// </summary>
        public IUserData UserData => _profileManager.UserData ?? throw new Exception("The user's data has not yet been loaded.");

        /// <summary>
        /// Gets the user's ID.
        /// </summary>
        public long UserId => UserData.Id;

        /// <summary>
        /// Gets the user's name.
        /// </summary>
        public string UserName => UserData.Name;

        /// <summary>
        /// Gets the user's gender.
        /// </summary>
        public Gender UserGender => UserData.Gender;

        /// <summary>
        /// Gets the user's figure.
        /// </summary>
        public string UserFigure => UserData.Figure;

        /// <summary>
        /// Gets the user's motto.
        /// </summary>
        public string UserMotto => UserData.Motto;

        /// <summary>
        /// Gets whether the user's name can be changed.
        /// </summary>
        public bool IsNameChangeable => UserData.IsNameChangeable;

        /// <summary>
        /// Gets the user's achievements.
        /// </summary>
        public IAchievements Achievements
        {
            get => _profileManager.Achievements ?? throw new Exception("The user's achievements have not yet been loaded.");
        }

        /// <summary>
        /// Sets the user's motto.
        /// </summary>
        /// <param name="motto">The motto to set.</param>
        public void SetMotto(string motto) => Send(Out.ChangeAvatarMotto, motto);

        /// <summary>
        /// Sets the user's figure.
        /// </summary>
        /// <param name="figureString">The figure string.</param>
        /// <param name="gender">The gender of the figrue.</param>
        public void SetFigure(string figureString, Gender gender)
            => Send(Out.UpdateAvatar, gender.ToShortString(), figureString);

        /// <summary>
        /// Sets the user's figure, inferring the gender from the figure string.
        /// </summary>
        /// <param name="figureString">The figure string.</param>
        public void SetFigure(string figureString)
        {
            var figure = Figure.Parse(figureString);
            if (figure.Gender == Gender.Unisex)
                throw new Exception($"Unable to detect gender for figure string: {figureString}");
            SetFigure(figure.GetFigureString(), figure.Gender);
        }

        /// <summary>
        /// Gets the list of badges owned by the user.
        /// </summary>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public List<Badge> GetBadges(int timeout = DEFAULT_TIMEOUT)
            => new GetBadgesTask(Interceptor).Execute(timeout, Ct);

        /// <summary>
        /// Gets the list of groups the user belongs to.
        /// </summary>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public List<GroupInfo> GetGroups(int timeout = DEFAULT_TIMEOUT)
        {
            var receiveTask = ReceiveAsync(In.GuildMemberships, timeout);
            Send(Out.GetGuildMemberships);
            var packet = receiveTask.GetAwaiter().GetResult();

            var list = new List<GroupInfo>();
            int n = packet.ReadInt();
            for (int i = 0; i < n; i++)
                list.Add(GroupInfo.Parse(packet));
            return list;
        }

        /// <summary>
        /// Gets the list of achievements of the user.
        /// </summary>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IAchievements GetAchievements(int timeout = DEFAULT_TIMEOUT)
            => new GetAchievementsTask(Interceptor).Execute(timeout, Ct);

        /// <summary>
        /// Gets the user's rooms.
        /// </summary>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IEnumerable<IRoomInfo> GetRooms(int timeout = DEFAULT_TIMEOUT)
            => SearchNav("my", "", timeout);
        #endregion

        #region - Currency -
        /// <summary>
        /// Gets the user's current credits.
        /// </summary>
        public int Credits => _profileManager.Credits ?? throw new Exception("User's credits have not yet been loaded.");

        /// <summary>
        /// Gets the user's activity points.
        /// </summary>
        public ActivityPoints Points => _profileManager.Points ?? throw new Exception("User's points have not yet been loaded.");

        /// <summary>
        /// Gets the user's current diamonds.
        /// </summary>
        public int Diamonds => Points[ActivityPointType.Diamond];

        /// <summary>
        /// Gets the user's current duckets.
        /// </summary>
        public int Duckets => Points[ActivityPointType.Ducket];
        #endregion


        #region - Rooms -
        /// <summary>
        /// Ensures the user is in a room, and displays an error message
        /// in the output log if the user is not in a room or has not re-entered
        /// the room after opening the scripter.
        /// </summary>
        public void AssertRoom()
        {
            if (!IsInRoom)
            {
                throw new ScriptException(
                    "This script requires you to be in a room. " +
                    "If you opened the scripter after you entered a room " +
                    "you will need to re-enter to initialize the room state."
                );
            }
        }

        /// <summary>
        /// Gets the data of the specified room.
        /// </summary>
        /// <param name="roomId">The ID of the room.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IRoomData GetRoomData(int roomId, int timeout = DEFAULT_TIMEOUT)
            => new GetRoomDataTask(Interceptor, roomId).Execute(timeout, Ct);

        /// <summary>
        /// Sends a request to create a room with the specified parameters.
        /// </summary>
        public void CreateRoom(string name, string description, string model,
            RoomCategory category, int maxUsers, TradePermissions trading)
        {
            Send(
                Out.CreateNewFlat,
                name, description, model,
                (int)category, maxUsers, (int)trading
            );
        }

        /// <summary>
        /// Gets the room settings of the specified room.
        /// </summary>
        /// <param name="roomId">The ID of the room.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        /// <returns></returns>
        public RoomSettings GetRoomSettings(int roomId, int timeout = DEFAULT_TIMEOUT) =>
            new GetRoomSettingsTask(Interceptor, roomId).Execute(timeout, Ct);

        /// <summary>
        /// Saves the specified room settings.
        /// </summary>
        public void SaveRoomSettings(RoomSettings settings) => Send(Out.SaveRoomSettings, settings);

        /// <summary>
        /// Sends a request to delete a room with the specified ID.
        /// </summary>
        public void DeleteRoom(long roomId) => Send(Out.DeleteFlat, roomId);

        /// <summary>
        /// Attempts to enter the room with the specified ID, and optionally, a password.
        /// </summary>
        public RoomEntryResult EnsureEnterRoom(long roomId, string password = "", int timeout = DEFAULT_TIMEOUT)
            => new EnterRoomTask(Interceptor, roomId, password).Execute(timeout, Ct);
            // => Send(Out.FlatOpc, (LegacyLong)roomId, password, 0, -1, -1);

        /// <summary>
        /// Leaves the room.
        /// </summary>
        public void LeaveRoom() => Send(Out.Quit);

        /// <summary>
        /// Gets the list of users with rights to the current room.
        /// Returns <c>null</c> if the user is not in a room, or is not the current room owner.
        /// </summary>
        public IReadOnlyList<(long Id, string Name)> GetRights(int timeout = DEFAULT_TIMEOUT)
        {
            if (!IsInRoom)
                throw new Exception("The user is not in a room.");
            return GetRightsFor(RoomId, timeout);
        }

        /// <summary>
        /// Gets the list of users with rights to the specified room.
        /// This operation will timeout if the user is not the owner of the specified room.
        /// </summary>
        public IReadOnlyList<(long Id, string Name)> GetRightsFor(long roomId, int timeout = DEFAULT_TIMEOUT)
            => new GetRightsListTask(Interceptor, roomId).Execute(timeout, Ct);

        /*
            TODO
                RemoveAllRights(int roomId)

                GetBannedUsers(int roomId)

                Mood light stuff

                GetRoomWordFilter()
                SetRoomBackground(...)

                MuteRoom(...)

                SearchRooms(string)
                SearchRoomsByTag(string)

                GetRoomsWithFriends()
                GetFriendsRooms()
                GetRoomsInGroup()
                GetRoomHistory()
                GetFavoriteRooms()
                GetRoomsWithRights()

                SetHomeRoom()
        */
        #endregion

        #region - Furni interaction -
        /// <summary>
        /// Uses the specified furni.
        /// </summary>
        public void UseFurni(IFurni furni)
        {
            if (furni.Type == ItemType.Floor)
            {
                UseFloorItem(furni.Id);
            }
            else if (furni.Type == ItemType.Wall)
            {
                UseWallItem(furni.Id);
            }
            else
            {
                throw new Exception($"Unknown furni type: {furni.Type}");
            }
        }

        /// <summary>
        /// Uses the specified floor item.
        /// </summary>
        public void UseFloorItem(long itemId) => ToggleFloorItem(itemId, 0);

        /// <summary>
        /// Uses the specified wall item.
        /// </summary>
        public void UseWallItem(long itemId) => ToggleWallItem(itemId, 0);

        /// <summary>
        /// Toggles the state of the specified furni.
        /// </summary>
        public void ToggleFurni(IFurni furni, int state)
        {
            if (furni.Type == ItemType.Floor)
            {
                ToggleFloorItem(furni.Id, state);
            }
            else if (furni.Type == ItemType.Wall)
            {
                ToggleWallItem(furni.Id, state);
            }
        }

        /// <summary>
        /// Toggles the state of the specified floor item.
        /// </summary>
        public void ToggleFloorItem(long itemId, int state) => Send(Out.UseStuff, (LegacyLong)itemId, state);

        /// <summary>
        /// Toggles the state of the specified wall item.
        /// </summary>
        public void ToggleWallItem(long itemId, int state) => Send(Out.UseWallItem, (LegacyLong)itemId, state);

        /// <summary>
        /// Uses the specified one-way gate.
        /// </summary>
        public void UseGate(long itemId) => Send(Out.EnterOneWayDoor, (LegacyLong)itemId);

        /// <summary>
        /// Uses the specified one-way gate.
        /// </summary>
        public void UseGate(IFloorItem item) => UseGate(item.Id);

        /// <summary>
        /// Deletes the specified wall item. Used for stickies, photos.
        /// </summary>
        public void DeleteWallItem(IWallItem item) => DeleteWallItem(item.Id);

        /// <summary>
        /// Deletes the specified wall item. Used for stickies, photos.
        /// </summary>
        public void DeleteWallItem(long itemId) => Send(Out.RemoveItem, (LegacyLong)itemId);

        /// <summary>
        /// Places a floor item at the specified location.
        /// </summary>
        public void Place(IInventoryItem item, int x, int y, int dir = 0)
        {
            if (item.Type != ItemType.Floor)
                throw new InvalidOperationException("The item is not a floor item.");
            PlaceFloorItem(item.ItemId, x, y, dir);
        }

        /// <summary>
        /// Places a floor item at the specified location.
        /// </summary>
        public void Place(IInventoryItem item, (int X, int Y) location, int dir = 0)
            => Place(item, location.X, location.Y, dir);

        /// <summary>
        /// Places a floor item at the specified location.
        /// </summary>
        public void Place(IInventoryItem item, Tile location, int dir = 0)
            => Place(item, location.X, location.Y, dir);

        /// <summary>
        /// Places a wall item at the specified location.
        /// </summary>
        public void Place(IInventoryItem item, WallLocation location)
        {
            if (item.Type != ItemType.Wall)
                throw new InvalidOperationException("Item is not a wall item.");
            PlaceWallItem(item.ItemId, location);
        }

        /// <summary>
        /// Moves a floor item to the specified location.
        /// </summary>
        public void Move(IFloorItem item, int x, int y, int dir = 0) => MoveFloorItem(item.Id, x, y, dir);

        /// <summary>
        /// Moves a floor item to the specified location.
        /// </summary>
        public void Move(IFloorItem item, (int X, int Y) location, int dir = 0) => MoveFloorItem(item.Id, location.X, location.Y, dir);

        /// <summary>
        /// Moves a floor item to the specified location.
        /// </summary>
        public void Move(IFloorItem item, Tile location, int dir = 0) => MoveFloorItem(item.Id, location.X, location.Y, dir);

        /// <summary>
        /// Moves a wall item to the specified location.
        /// </summary>
        public void Move(IWallItem item, WallLocation location) => MoveWallItem(item.Id, location);

        /// <summary>
        /// Moves a wall item to the specified location.
        /// </summary>
        public void Move(IWallItem item, string location) => MoveWallItem(item.Id, location);

        /// <summary>
        /// Picks up the specified furni.
        /// </summary>
        public void Pickup(IFurni furni)
        {
            if (furni.Type == ItemType.Floor)
            {
                PickupFloorItem(furni.Id);
            }
            else
            {
                PickupWallItem(furni.Id);
            }
        }

        /// <summary>
        /// Places a floor item at the specified location.
        /// </summary>
        public void PlaceFloorItem(long itemId, int x, int y, int dir = 0)
        {
            if (ClientType == ClientType.Flash)
            {
                Send(Out.PlaceRoomItem, $"{itemId} {x} {y} {dir}");
            }
            else
            {
                Send(Out.PlaceRoomItem, itemId, x, y, dir);
            }
        }

        /// <summary>
        /// Moves a floor item to the specified location.
        /// </summary>
        public void MoveFloorItem(long itemId, int x, int y, int dir = 0) => Send(Out.MoveRoomItem, (LegacyLong)itemId, x, y, dir);

        /// <summary>
        /// Picks up the specified floor item.
        /// </summary>
        public void PickupFloorItem(long itemId) => Send(Out.PickItemUpFromRoom, 2, (LegacyLong)itemId);

        /// <summary>
        /// Places a wall item at the specified location.
        /// </summary>
        public void PlaceWallItem(long itemId, WallLocation location)
        {
            switch (ClientType)
            {
                case ClientType.Flash: Send(Out.PlaceRoomItem, $"{itemId} {location}"); break;
                case ClientType.Unity: Send(Out.PlaceWallItem, itemId, location); break;
                default: throw new Exception("Unknown client protocol.");
            }
        }

        /// <summary>
        /// Places a wall item at the specified location.
        /// </summary>
        public void PlaceWallItem(long itemId, string location) => PlaceWallItem(itemId, WallLocation.Parse(location));

        /// <summary>
        /// Moves a wall item to the specified location.
        /// </summary>
        public void MoveWallItem(long itemId, WallLocation location) => Send(Out.MoveWallItem, (LegacyLong)itemId, location);

        /// <summary>
        /// Moves a wall item to the specified location.
        /// </summary>
        public void MoveWallItem(long itemId, string location) => MoveWallItem(itemId, WallLocation.Parse(location));

        /// <summary>
        /// Picks up the specified wall item.
        /// </summary>
        public void PickupWallItem(long itemId) => Send(Out.PickItemUpFromRoom, 1, (LegacyLong)itemId);

        /// <summary>
        /// Updates the stack tile to the specified height.
        /// </summary>
        public void UpdateStackTile(IFloorItem stackTile, double height) => UpdateStackTile(stackTile.Id, height);
        /// <summary>
        /// Updates the stack tile to the specified height.
        /// </summary>
        public void UpdateStackTile(long stackTileId, double height)
            => Send(Out.StackingHelperSetCaretHeight, (LegacyLong)stackTileId, (int)Math.Round(height * 100.0));
        #endregion

        #region - Entity interaction -
        /// <summary>
        /// Ignores the specified user.
        /// </summary>
        public void Ignore(IRoomUser user) => Ignore(user.Name);

        /// <summary>
        /// Ignores the specified user.
        /// </summary>
        public void Ignore(string name) => Send(Out.IgnoreUser, name);

        /// <summary>
        /// Unignores the specified user.
        /// </summary>
        public void Unignore(IRoomUser user) => Unignore(user.Name);

        /// <summary>
        /// Unignores the specified user.
        /// </summary>
        public void Unignore(string name) => Send(Out.UnignoreUser, name);

        /// <summary>
        /// Sends a friend request to the specified user.
        /// </summary>
        public void FriendRequest(IRoomUser user) => FriendRequest(user.Name);

        /// <summary>
        /// Sends a friend request to the specified user.
        /// </summary>
        public void FriendRequest(string name) => Send(Out.RequestFriend, name);

        /// <summary>
        /// Respects the specified user.
        /// </summary>
        public void Respect(long userId) => Send(Out.RespectUser, userId);

        /// <summary>
        /// Respects the specified user.
        /// </summary>
        public void Respect(IRoomUser user) => Respect(user.Id);

        /// <summary>
        /// Scratches (or treats) the specified pet.
        /// </summary>
        public void Scratch(long petId) => Send(Out.RespectPet, petId);

        /// <summary>
        /// Scratches (or treats) the specified pet.
        /// </summary>
        public void Scratch(IPet pet) => Scratch(pet.Id);

        /// <summary>
        /// Mounts or dismounts the pet with the specified id.
        /// </summary>
        /// <param name="petId">The id of the pet to (dis)mount.</param>
        /// <param name="mount">Whether to mount or dismount.</param>
        public void Ride(long petId, bool mount) => Send(Out.MountPet, petId, mount);

        /// <summary>
        /// Mounts or dismounts the specified pet.
        /// </summary>
        /// <param name="pet">The pet to (dis)mount.</param>
        /// <param name="mount">Whether to mount or dismount.</param>
        public void Ride(IPet pet, bool mount) => Ride(pet.Id, mount);

        /// <summary>
        /// Mounts the pet with the specified id.
        /// </summary>
        public void Mount(long petId) => Ride(petId, true);

        /// <summary>
        /// Mounts the specified pet.
        /// </summary>
        public void Mount(IPet pet) => Ride(pet.Id, true);

        /// <summary>
        /// Dismounts the pet with the specified id.
        /// </summary>
        public void Dismount(long petId) => Ride(petId, false);

        /// <summary>
        /// Dismounts the specified pet.
        /// </summary>
        public void Dismount(IPet pet) => Ride(pet.Id, false);
        #endregion

        #region - Groups -
        /// <summary>
        /// Joins the group with the specified ID.
        /// </summary>
        public void JoinGroup(long groupId) => Send(Out.JoinHabboGroup, groupId);

        /// <summary>
        /// Leaves the group with the specified ID. <see cref="Feature.UserData"/> must be available.
        /// </summary>
        [RequiredOut(nameof(Outgoing.KickMember))] // TODO RequiredFeature(Feature.UserData)
        public void LeaveGroup(long groupId) => Send(Out.KickMember, groupId, UserId, false);

        [RequiredOut(nameof(Outgoing.SelectFavouriteHabboGroup))]
        public void SetGroupFavorite(long groupId) => Send(Out.SelectFavouriteHabboGroup, groupId);

        [RequiredOut(nameof(Outgoing.DeselectFavouriteHabboGroup))]
        public void RemoveGroupFavorite(long groupId) => Send(Out.DeselectFavouriteHabboGroup, groupId);

        /// <summary>
        /// Gets the group information of the specified group.
        /// </summary>
        /// <param name="groupId">The group ID.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IGroupData GetGroup(long groupId, int timeout = DEFAULT_TIMEOUT) =>
            new GetGroupDataTask(Interceptor, groupId).Execute(timeout, Ct);

        /// <summary>
        /// Gets a paged list of group members in the specified group.
        /// </summary>
        /// <param name="groupId">The group ID.</param>
        /// <param name="page">The page number. (starting at 0)</param>
        /// <param name="filter">The filter text.</param>
        /// <param name="searchType">The type of member to search for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IGroupMembers GetGroupMembers(long groupId, int page = 0, string filter = "",
            GroupMemberSearchType searchType = GroupMemberSearchType.Members, int timeout = DEFAULT_TIMEOUT)
            => new GetGroupMembersTask(Interceptor, groupId, page, filter, searchType).Execute(timeout, Ct);

        [RequiredOut(nameof(Outgoing.ApproveMembershipRequest))]
        public void AcceptGroupMember(long groupId, long userId) => Send(Out.ApproveMembershipRequest, groupId, userId);

        [RequiredOut(nameof(Outgoing.RejectMembershipRequest))]
        public void DeclineGroupMember(long groupId, long userId) => Send(Out.RejectMembershipRequest, groupId, userId);

        [RequiredOut(nameof(Outgoing.KickMember))]
        public void RemoveGroupMember(long groupId, long userId) => Send(Out.KickMember, groupId, userId, false); // ?
        #endregion

        #region - Tasks -
        /*
            TODO
                GetUserInfoWeb(int userId)
                GetUserProfileWeb(string uniqueId)

                GetOwnGroups()
                GetOwnRooms()

                GetPetInfo(Pet)
                GetPetInfo(petId)
        */

        public UserSearchResult? SearchUser(string name, int timeout = DEFAULT_TIMEOUT)
            => new SearchUserTask(Interceptor, name).Execute(timeout, Ct).GetResult(name);

        public UserSearchResults SearchUsers(string name, int timeout = DEFAULT_TIMEOUT)
            => new SearchUserTask(Interceptor, name).Execute(timeout, Ct);

        /// <summary>
        /// Gets the profile of the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IUserProfile GetProfile(int userId, int timeout = DEFAULT_TIMEOUT)
            => new GetProfileTask(Interceptor, userId).Execute(timeout, Ct);
        #endregion

        

        #region - Events -
        private void Register<TEventSource, TEventArgs>(TEventSource source, string eventName, Func<TEventArgs, Task> callback)
            where TEventArgs : EventArgs
        {
            if (source is null)
                throw new Exception($"Component is unavailable: {typeof(TEventSource).Name}");

            EventInfo eventInfo = typeof(TEventSource).GetEvent(eventName) ?? throw new Exception($"Unable to get EventInfo for event: {eventName}");

            Delegate handler;
            if (eventInfo.EventHandlerType == typeof(EventHandler))
            {
                if (typeof(TEventArgs) != typeof(EventArgs))
                    throw new InvalidOperationException("EventHandler must use System.EventArgs as its argument type.");

                handler = new EventHandler((s, e) => callback((TEventArgs)e));
            }
            else
            {
                handler = new EventHandler<TEventArgs>((s, e) => callback(e));
            }

            eventInfo.AddEventHandler(source, handler);

            lock (_disposables)
            {
                _disposables.Add(new Unsubscriber(source, eventInfo, handler));
            }
        }

        private void Register<TEventSource, TEventArgs>(TEventSource source, string eventName, Action<TEventArgs> callback)
            where TEventArgs : EventArgs
        {
            Register<TEventSource, TEventArgs>(source, eventName, e => { callback(e); return Task.CompletedTask; });
        }

        #region - Room events -
        /*/// <summary>
        /// Registers a callback that is invoked when the user rings the doorbell to a room.
        /// </summary>
        public void OnRingDoorbell(Action<EventArgs> callback) => Register(_roomManager, nameof(_roomManager.RingingDoorbell), callback);
        /// <summary>
        /// Registers a callback callback that is invoked when the user rings the doorbell to a room.
        /// </summary>
        public void OnRingDoorbell(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.RingingDoorbell), callback);*/

        /// <summary>
        /// Registers a callback that is invoked when the user enters the room queue.
        /// </summary>
        public void OnEnteredQueue(Action<EventArgs> callback) => Register(_roomManager, nameof(_roomManager.EnteredQueue), callback);
        /// <summary>
        /// Registers a callback that is invoked when the user enters the room queue.
        /// </summary>
        public void OnEnteredQueue(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EnteredQueue), callback);

        /// <summary>
        /// Registers a callback that is invoked when the user's queue position changes.
        /// </summary>
        public void OnQueueUpdate(Action<EventArgs> callback) => Register(_roomManager, nameof(_roomManager.QueuePositionUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when the user's queue position changes.
        /// </summary>
        public void OnQueueUpdate(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.QueuePositionUpdated), callback);

        /// <summary>
        /// Registers a callback that is invoked when the user is entering a room.
        /// </summary>
        public void OnEnteringRoom(Action<EventArgs> callback) => Register(_roomManager, nameof(_roomManager.Entering), callback);
        /// <summary>
        /// Registers a callback that is invoked when the user is entering a room.
        /// </summary>
        public void OnEnteringRoom(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.Entering), callback);

        /// <summary>
        /// Registers a callback that is invoked when the user has entered a room.
        /// </summary>
        public void OnEnteredRoom(Action<RoomEventArgs> callback) => Register(_roomManager, nameof(_roomManager.Entered), callback);
        /// <summary>
        /// Registers a callback that is invoked when the user has entered a room.
        /// </summary>
        public void OnEnteredRoom(Func<RoomEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.Entered), callback);

        /// <summary>
        /// Registers a callback that is invoked when the user has left room.
        /// </summary>
        public void OnLeftRoom(Action<EventArgs> callback) => Register(_roomManager, nameof(_roomManager.Left), callback);
        /// <summary>
        /// Registers a callback that is invoked when the user has left room.
        /// </summary>
        public void OnLeftRoom(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.Left), callback);

        /// <summary>
        /// Registers a callback that is invoked when the user is kicked from a room.
        /// </summary>
        public void OnKicked(Action<EventArgs> callback) => Register(_roomManager, nameof(_roomManager.Kicked), callback);
        /// <summary>
        /// Registers a callback that is invoked when the user is kicked from a room.
        /// </summary>
        public void OnKicked(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.Kicked), callback);

        /// <summary>
        /// Registers a callback that is invoked when the room data updates.
        /// </summary>
        public void OnRoomDataUpdate(Action<RoomDataEventArgs> callback) => Register(_roomManager, nameof(_roomManager.RoomDataUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when the room data updates.
        /// </summary>
        public void OnRoomDataUpdate(Func<RoomDataEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.RoomDataUpdated), callback);

        /*/// <summary>
        /// Registers a callback that is invoked when someone rings the doorbell.
        /// </summary>
        public void OnDoorbell(Action<DoorbellEventArgs> callback) => Register(_roomManager, nameof(_roomManager.Doorbell), callback);
        /// <summary>
        /// Registers a callback that is invoked when someone rings the doorbell.
        /// </summary>
        public void OnDoorbell(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.Doorbell), callback);*/
        #endregion

        #region - Furni events -
        /// <summary>
        /// Registers a callback that is invoked when a room's floor items are first loaded.
        /// </summary>
        public void OnFloorItemsLoaded(Action<FloorItemsEventArgs> callback) => Register(_roomManager, nameof(_roomManager.FloorItemsLoaded), callback);
        /// <summary>
        /// Registers a callback that is invoked when a room's floor items are first loaded.
        /// </summary>
        public void OnFloorItemsLoaded(Func<FloorItemsEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.FloorItemsLoaded), callback);

        /// <summary>
        /// Registers a callback that is invoked when a floor item is placed in the room.
        /// </summary>
        public void OnFloorItemAdded(Action<FloorItemEventArgs> callback) => Register(_roomManager, nameof(_roomManager.FloorItemAdded), callback);
        /// <summary>
        /// Registers a callback that is invoked when a floor item is placed in the room.
        /// </summary>
        public void OnFloorItemAdded(Func<FloorItemEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.FloorItemAdded), callback);

        /// <summary>
        /// Registers a callback that is invoked when a floor item is updated.
        /// This happens when the floor item is moved or rotated.
        /// </summary>
        public void OnFloorItemUpdated(Action<FloorItemUpdatedEventArgs> callback) => Register(_roomManager, nameof(_roomManager.FloorItemUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when a floor item is updated.
        /// This happens when the floor item is moved or rotated.
        /// </summary>
        public void OnFloorItemUpdated(Func<FloorItemUpdatedEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.FloorItemUpdated), callback);

        /// <summary>
        /// Registers a callback that is invoked when a floor item's data is updated.
        /// This happens when the state of a floor item is changed,
        /// for example a gate opening/closing or an animation state changing.
        /// </summary>
        public void OnFloorItemDataUpdated(Action<FloorItemDataUpdatedEventArgs> callback) => Register(_roomManager, nameof(_roomManager.FloorItemDataUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when a floor item's data is updated.
        /// This happens when the state of a floor item is changed,
        /// for example a gate opening/closing or an animation state changing.
        /// </summary>
        public void OnFloorItemDataUpdated(Func<FloorItemDataUpdatedEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.FloorItemDataUpdated), callback);

        /// <summary>
        /// Registers a callback that is invoked when a floor item slides on a roller, or due to a wired trigger.
        /// </summary>
        public void OnFloorItemSlide(Action<FloorItemSlideEventArgs> callback) => Register(_roomManager, nameof(_roomManager.FloorItemSlide), callback);
        /// <summary>
        /// Registers a callback that is invoked when a floor item slides on a roller, or due to a wired trigger.
        /// </summary>
        public void OnFloorItemSlide(Func<FloorItemSlideEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.FloorItemSlide), callback);

        /// <summary>
        /// Registers a callback that is invoked when a floor item is removed from the room.
        /// </summary>
        public void OnFloorItemRemoved(Action<FloorItemEventArgs> callback) => Register(_roomManager, nameof(_roomManager.FloorItemRemoved), callback);
        /// <summary>
        /// Registers a callback that is invoked when a floor item is removed from the room.
        /// </summary>
        public void OnFloorItemRemoved(Func<FloorItemEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.FloorItemRemoved), callback);

        /// <summary>
        /// Registers a callback that is invoked when a room's wall items are first loaded.
        /// </summary>
        public void OnWallItemsLoaded(Action<WallItemsEventArgs> callback) => Register(_roomManager, nameof(_roomManager.WallItemsLoaded), callback);
        /// <summary>
        /// Registers a callback that is invoked when a room's wall items are first loaded.
        /// </summary>
        public void OnWallItemsLoaded(Func<WallItemsEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.WallItemsLoaded), callback);

        /// <summary>
        /// Registers a callback that is invoked when a wall item is placed in the room.
        /// </summary>
        public void OnWallItemAdded(Action<WallItemEventArgs> callback) => Register(_roomManager, nameof(_roomManager.WallItemAdded), callback);
        /// <summary>
        /// Registers a callback that is invoked when a wall item is placed in the room.
        /// </summary>
        public void OnWallItemAdded(Func<WallItemEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.WallItemAdded), callback);

        /// <summary>
        /// Registers a callback that is invoked when a wall item is updated.
        /// </summary>
        public void OnWallItemUpdated(Action<WallItemUpdatedEventArgs> callback) => Register(_roomManager, nameof(_roomManager.WallItemUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when a wall item is updated.
        /// </summary>
        public void OnWallItemUpdated(Func<WallItemUpdatedEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.WallItemUpdated), callback);

        /// <summary>
        /// Registers a callback that is invoked when a wall item is removed from the room.
        /// </summary>
        public void OnWallItemRemoved(Action<WallItemEventArgs> callback) => Register(_roomManager, nameof(_roomManager.WallItemRemoved), callback);
        /// <summary>
        /// Registers a callback that is invoked when a wall item is removed from the room.
        /// </summary>
        public void OnWallItemRemoved(Func<WallItemEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.WallItemRemoved), callback);

        #endregion

        #region - Entity events -
        /// <summary>
        /// Registers a callback that is invoked when an entity is added to the room.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEntityAdded(Action<EntityEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityAdded), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity is added to the room.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEntityAdded(Func<EntityEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityAdded), callback);

        /// <summary>
        /// Registers a callback that is invoked when entities are added to the room.
        /// </summary>
        public void OnEntitiesAdded(Action<EntitiesEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntitiesAdded), callback);
        /// <summary>
        /// Registers a callback that is invoked when entities are added to the room.
        /// </summary>
        public void OnEntitiesAdded(Func<EntitiesEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntitiesAdded), callback);

        /// <summary>
        /// Registers a callback that is invoked when an entity is updated.
        /// </summary>
        public void OnEntityUpdated(Action<EntityEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity is updated.
        /// </summary>
        public void OnEntityUpdated(Func<EntityEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityUpdated), callback);

        /// <summary>
        /// Registers a callback that is invoked when an entity slides on a roller.
        /// </summary>
        public void OnEntitySlide(Action<EntitySlideEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntitySlide), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity slides on a roller.
        /// </summary>
        public void OnEntitySlide(Func<EntitySlideEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntitySlide), callback);

        /// <summary>
        /// Registers a callback that is invoked when a user's figure, gender, motto or achievement score is updated.
        /// </summary>
        /// <param name="callback"></param>
        public void OnUserDataUpdated(Action<EntityDataUpdatedEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityDataUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when a user's figure, gender, motto or achievement score is updated.
        /// </summary>
        /// <param name="callback"></param>
        public void OnUserDataUpdated(Func<EntityDataUpdatedEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityDataUpdated), callback);

        /// <summary>
        /// Registers a callback that is invoked when an entity's idle status changes.
        /// </summary>
        public void OnEntityIdle(Action<EntityIdleEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityIdle), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity's idle status changes.
        /// </summary>
        public void OnEntityIdle(Func<EntityIdleEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityIdle), callback);

        /// <summary>
        /// Registers a callback that is invoked when an entity's dance changes.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEntityDance(Action<EntityDanceEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityDance), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity's dance changes.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEntityDance(Func<EntityDanceEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityDance), callback);

        /// <summary>
        /// Registers a callback to be invoked when an entity's hand item changes.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEntityHandItem(Action<EntityHandItemEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityHandItem), callback);
        /// <summary>
        /// Registers a callback to be invoked when an entity's hand item changes.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEntityHandItem(Func<EntityHandItemEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityHandItem), callback);

        /// <summary>
        /// Registers a callback that is invoked when an entity's effect changes.
        /// </summary>
        public void OnEntityEffect(Action<EntityEffectEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityEffect), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity's effect changes.
        /// </summary>
        public void OnEntityEffect(Func<EntityEffectEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityEffect), callback);

        /// <summary>
        /// Registers a callback that is invoked when an entity performs an action.
        /// </summary>
        public void OnEntityAction(Action<EntityActionEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityAction), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity performs an action.
        /// </summary>
        public void OnEntityAction(Func<EntityActionEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityAction), callback);

        /// <summary>
        /// Registers a callback that is invoked when an entity is removed from the room.
        /// </summary>
        public void OnEntityRemoved(Func<EntityEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityRemoved), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity is removed from the room.
        /// </summary>
        public void OnEntityRemoved(Action<EntityEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityRemoved), callback);
        #endregion

        #region - Chat events -
        /// <summary>
        /// Registers a callback that is invoked when an entity in the room chats.
        /// </summary>
        public void OnChat(Action<EntityChatEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityChat), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity in the room chats.
        /// </summary>
        public void OnChat(Func<EntityChatEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityChat), callback);
        #endregion
        #endregion
    }
}
