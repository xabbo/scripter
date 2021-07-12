using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.Interceptor.Tasks;
using Xabbo.Interceptor.Dispatcher;

using Xabbo.Core;
using Xabbo.Core.Events;
using Xabbo.Core.Game;
using Xabbo.Core.GameData;
using Xabbo.Core.Tasks;

using b7.Scripter.Runtime;
using b7.Scripter.Services;
using Xabbo;

namespace b7.Scripter.Scripting
{
    /// <summary>
    /// The b7 scripter globals class.
    /// Contains the methods and properties that are globally accessible from scripts.
    /// </summary>
    public class G : IDisposable
    {
        private const int DEFAULT_TIMEOUT = 10000;

        private readonly IScriptHost _scriptHost;
        private readonly IScript _script;

        private readonly List<IDisposable> _disposables = new();
        private readonly List<Intercept> _intercepts = new();

        private readonly CancellationTokenSource _cts;

        private IInterceptDispatcher _dispatcher => _scriptHost.Interceptor.Dispatcher;

        private ProfileManager _profileManager => _scriptHost.GameManager.ProfileManager;
        private FriendManager _friendManager => _scriptHost.GameManager.FriendManager;
        private RoomManager _roomManager => _scriptHost.GameManager.RoomManager;
        private TradeManager _tradeManager => _scriptHost.GameManager.TradeManager;

        public IInterceptor Interceptor => _scriptHost.Interceptor;
        public ClientType ClientType => Interceptor.ClientType;

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
        /// Returns a new <see cref="ScriptError"/> with the specified message
        /// which will be displayed in the log when thrown.
        /// </summary>
        public ScriptException Error(string message) => new ScriptException(message);

        /// <summary>
        /// Serializes an object to JSON.
        /// </summary>
        /// <typeparam name="TValue">The type of the object to serialize.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <param name="indented">Specifies whether to use indented formatting or not.</param>
        public string ToJson<TValue>(TValue? value, bool indented = true) => _scriptHost.JsonSerializer.Serialize(value, indented);

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
        /// Returns a non-negative random integer.
        /// </summary>
        public int Rand() => _scriptHost.Random.Next();

        /// <summary>
        /// Returns a non-negative integer that is less than the specified maximum.
        /// </summary>
        public int Rand(int max) => _scriptHost.Random.Next(max);

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        public int Rand(int min, int max) => _scriptHost.Random.Next(min, max);

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        public void Rand(byte[] buffer) => _scriptHost.Random.NextBytes(buffer);

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns></returns>
        public double RandDouble() => _scriptHost.Random.NextDouble();

        /// <summary>
        /// Returns a random element from a specified enumerable.
        /// </summary>
        public T Rand<T>(IEnumerable<T> enumerable)
        {
            if (enumerable is not Array array)
                array = enumerable.ToArray();
            return (T)(array.GetValue(Rand(array.Length)) ?? throw new NullReferenceException());
        }

        /// <summary>
        /// Returns a random element from a specified array.
        /// </summary>
        public T Rand<T>(T[] array) => array[Rand(array.Length)];

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

        #region - Metadata -
        /// <summary>
        /// Gets the information of the specified item from the furni data.
        /// </summary>
        public FurniInfo? GetInfo(IItem item) => FurniData.GetInfo(item);

        /// <summary>
        /// Gets the name of the specified item from the furni data.
        /// </summary>
        public string? GetName(IItem item) => FurniData.GetInfo(item)?.Name;

        /// <summary>
        /// Gets the category of the specified item from the furni data.
        /// </summary>
        public FurniCategory GetCategory(IItem item) => FurniData.GetInfo(item)?.Category ?? FurniCategory.Unknown;
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
        public void Send(Header header, params object[] values) => Send(Packet.Compose(Interceptor.ClientType, header, values));

        /// <summary>
        /// Sends the specified packet to the client or server.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void Send(IReadOnlyPacket packet) => Interceptor.Send(packet);

        /// <summary>
        /// Captures a packet being sent to the client with a header that matches any of the defined target headers.
        /// </summary>
        /// <param name="timeout">The time to wait for a packet to be received.</param>
        /// <param name="targetHeaders">The incoming message headers to listen for.</param>
        /// <returns>The first packet received with a header that matches one of the target headers.</returns>
        public IReadOnlyPacket Receive(int timeout, params Header[] targetHeaders)
            => ReceiveAsync(timeout, targetHeaders).GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronously captures a packet being sent to the client with a header that matches any of the defined target headers.
        /// </summary>
        /// <param name="timeout">The time to wait for a packet to be received.</param>
        /// <param name="targetHeaders">The incoming message headers to listen for.</param>
        /// <returns>The first packet received with a header that matches one of the target headers.</returns>
        public Task<IPacket> ReceiveAsync(int timeout, params Header[] targetHeaders)
        {
            AssertTargetHeaders(Destination.Client, targetHeaders);

            return new CaptureMessageTask(Interceptor, Destination.Client, false, targetHeaders)
                .ExecuteAsync(timeout, Ct);
        }

        /// <summary>
        /// Attempts to capture a packet being sent to the client with a header that matches any of the defined target headers.
        /// </summary>
        /// <param name="timeout">The time to wait for a packet to be received.</param>
        /// <param name="packet">The packet that was captured.</param>
        /// <param name="targetHeaders">The incoming message headers to listen for.</param>
        /// <returns>True if a packet was successfully captured, or false if the operation timed out.</returns>
        public bool TryReceive(int timeout, out IReadOnlyPacket? packet, params Header[] targetHeaders)
        {
            packet = null;
            try
            {
                packet = Receive(timeout, targetHeaders);
                return true;
            }
            catch (OperationCanceledException)
            when (!Ct.IsCancellationRequested)
            {
                return false;
            }
        }

        /// <summary>
        /// Captures a packet being sent to the server with a header that matches any of the defined target headers.
        /// </summary>
        /// <param name="timeout">The time to wait for a packet to be captured.</param>
        /// <param name="targetHeaders">The outgoing message headers to listen for.</param>
        /// <returns>The first packet captured with a header that matches one of the target headers.</returns>
        public IReadOnlyPacket CaptureOut(int timeout, params Header[] targetHeaders)
            => CaptureOutAsync(timeout, targetHeaders).GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronously a packet being sent to the server with a header that matches any of the defined target headers.
        /// </summary>
        /// <param name="timeout">The time to wait for a packet to be captured.</param>
        /// <param name="targetHeaders">The outgoing message headers to listen for.</param>
        /// <returns>The first packet captured with a header that matches one of the target headers.</returns>
        public Task<IPacket> CaptureOutAsync(int timeout, params Header[] targetHeaders)
        {
            AssertTargetHeaders(Destination.Server, targetHeaders);

            return new CaptureMessageTask(Interceptor, Destination.Server, false, targetHeaders)
                .ExecuteAsync(timeout, Ct);
        }

        /// <summary>
        /// Attempts to capture a packet being sent to the server with a header that matches any of the defined target headers.
        /// </summary>
        /// <param name="timeout">The time to wait for a packet to be captured.</param>
        /// <param name="packet">The packet that was captured.</param>
        /// <param name="targetHeaders">The outgoing message headers to listen for.</param>
        /// <returns>True if a packet was successfully captured, or false if the operation timed out.</returns>
        public bool TryCaptureOut(int timeout, out IReadOnlyPacket? packet, params Header[] targetHeaders)
        {
            packet = null;
            try { packet = CaptureOut(timeout, targetHeaders); return true; }
            catch (OperationCanceledException) when (!Ct.IsCancellationRequested) { return false; }
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
        public void OnIntercept(Header[] headers, Action<InterceptArgs> callback)
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
        public void OnIntercept(Header[] headers, Func<InterceptArgs, Task> callback)
           => OnIntercept(headers, e => { callback(e); });
        #endregion

        #region - Room -
        /// <summary>
        /// Gets the data of the room the user is currently in.
        /// </summary>
        public IRoomData? RoomData => _roomManager.Data;

        /// <summary>
        /// Gets the ID of the current/last room that the user is/was in.
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
        /// Gets the height map of the room.
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
        /// Gets if the user's name can be changed.
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
            var receiveTask = ReceiveAsync(timeout, In.GuildMemberships);
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

        #region - Friends -
        public IEnumerable<IFriend> Friends => _friendManager.Friends;

        /// <summary>
        /// Accepts a friend request from the specified user.
        /// </summary>
        public void AcceptFriendRequest(long userId) => AcceptFriendRequests(userId);

        /// <summary>
        /// Accepts friend requests from the specified users.
        /// </summary>
        public void AcceptFriendRequests(params long[] userIds) => Send(Out.AcceptFriend, userIds);

        /// <summary>
        /// Declines a friend request from the specified user.
        /// </summary>
        public void DeclineFriendRequest(long userId) => DeclineFriendRequests(userId);

        /// <summary>
        /// Declines friend requests from the specified users.
        /// </summary>
        public void DeclineFriendRequests(params long[] userIds) => Send(Out.DeclineFriend, false, userIds);

        /// <summary>
        /// Declines all incoming friend requests.
        /// </summary>
        public void DeclineAllFriendRequests() => Send(Out.DeclineFriend, true, 0);

        /// <summary>
        /// Sends a friend request to the specified user.
        /// </summary>
        public void AddFriend(string name) => Send(Out.RequestFriend, name);

        /// <summary>
        /// Removes the specified user from the user's friend list.
        /// </summary>
        public void RemoveFriend(long userId) => RemoveFriends(userId);

        /// <summary>
        /// Removes the specified users from the user's friend list.
        /// </summary>
        public void RemoveFriends(params long[] userIds) => Send(Out.RemoveFriend, userIds);

        /// <summary>
        /// Sends a private message to a friend with the specified ID.
        /// </summary>
        public void SendMessage(long userId, string message) => Send(Out.SendMessage, userId, message);
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
        /// Sends a request to enter a room with the specified ID, and optionally, a password.
        /// </summary>
        public void EnterRoom(long roomId, string password = "") => Send(Out.FlatOpc, roomId, password, 0, 0);

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

        #region - Actions -
        /// <summary>
        /// Makes the user perform the specified action.
        /// </summary>
        public void Action(int action) => Send(Out.Expression, action);

        /// <summary>
        /// Makes the user perform the specified action.
        /// </summary>
        public void Action(Actions action) => Action((int)action);

        /// <summary>
        /// Makes the user unidle.
        /// </summary>
        public void Unidle() => Action(Actions.None);

        /// <summary>
        /// Makes the user wave.
        /// </summary>
        public void Wave() => Action(Actions.Wave);

        /// <summary>
        /// Makes the user idle.
        /// </summary>
        public void Idle() => Action(Actions.Idle);

        /// <summary>
        /// Makes the user thumbs up.
        /// </summary>
        public void ThumbsUp() => Action(Actions.ThumbsUp);

        /// <summary>
        /// Makes the user sit if <c>true</c>, or stand if <c>false</c> is passed in.
        /// </summary>
        /// <param name="sit"><c>true</c> to sit, or <c>false</c> to stand.</param>
        public void Sit(bool sit) => Send(Out.Posture, sit ? 1 : 0);

        /// <summary>
        /// Makes the user sit.
        /// </summary>
        public void Sit() => Send(Out.Posture, 1);

        /// <summary>
        /// Makes the user stand.
        /// </summary>
        public void Stand() => Send(Out.Posture, 0);

        /// <summary>
        /// Makes the user show the specified sign.
        /// </summary>
        public void Sign(int sign) => Send(Out.ShowSign, sign);

        /// <summary>
        /// Makes the user show the specified sign.
        /// </summary>
        public void Sign(Signs sign) => Sign((int)sign);

        /// <summary>
        /// Makes the user perform the specfied dance.
        /// </summary>
        public void Dance(int dance) => Send(Out.Dance, dance);

        /// <summary>
        /// Makes the user perform the specfied dance.
        /// </summary>
        public void Dance(Dances dance) => Dance((int)dance);

        /// <summary>
        /// Makes the user dance.
        /// </summary>
        public void Dance() => Dance(1);

        /// <summary>
        /// Makes the user stop dancing.
        /// </summary>
        public void StopDancing() => Dance(0);
        #endregion

        #region - Effects -
        /// <summary>
        /// Activates the specified effect. (Warning: this will consume the effect if it is not permanent)
        /// </summary>
        public void ActivateEffect(int effectId) => Send(Out.ActivateAvatarEffect, effectId);

        /// <summary>
        /// Enables the specified effect.
        /// </summary>
        public void EnableEffect(int effectId) => Send(Out.UseAvatarEffect, effectId);

        /// <summary>
        /// Disables the current effect.
        /// </summary>
        public void DisableEffect() => EnableEffect(-1);
        #endregion

        #region - Movement -
        /// <summary>
        /// Moves to the specified location.
        /// </summary>
        public void Move(int x, int y) => Send(Out.Move, x, y);

        /// <summary>
        /// Moves to the specified location.
        /// </summary>
        public void Move((int X, int Y) location) => Move(location.X, location.Y);

        /// <summary>
        /// Moves to the specified location.
        /// </summary>
        public void Move(Tile location) => Move(location.X, location.Y);

        /// <summary>
        /// Makes the user look to the specified location.
        /// </summary>
        public void LookTo(int x, int y) => Send(Out.LookTo, x, y);

        /// <summary>
        /// Makes the user look to the specified location.
        /// </summary>
        public void LookTo((int X, int Y) location) => LookTo(location.X, location.Y);

        /// <summary>
        /// Makes the user look to the specified location.
        /// </summary>
        public void LookTo(Tile location) => LookTo(location.X, location.Y);

        /// <summary>
        /// Makes the user look to the specified direction.
        /// </summary>
        public void Turn(int dir) => LookTo(H.GetMagicVector(dir));

        /// <summary>
        /// Makes the user look to the specified direction.
        /// </summary>
        public void Turn(Directions dir) => Turn((int)dir);
        #endregion

        #region - Chat -
        /// <summary>
        /// Sends a chat message with the specified message and chat bubble style.
        /// </summary>
        public void Chat(ChatType chatType, string message, int bubble = 0)
        {
            switch (chatType)
            {
                case ChatType.Talk:
                    Send(Out.Chat, message, bubble, -1);
                    break;
                case ChatType.Shout:
                    Send(Out.Shout, message, bubble);
                    break;
                case ChatType.Whisper:
                    Send(Out.Whisper, message, bubble, -1);
                    break;
                default:
                    throw new Exception($"Unknown chat type: {chatType}.");
            }
        }

        /// <summary>
        /// Whispers a user with the specified message and chat bubble style.
        /// </summary>
        public void Whisper(RoomUser recipient, string message, int bubble = 0)
            => Chat(ChatType.Whisper, $"{recipient.Name} {message}", bubble);

        /// <summary>
        /// Whispers a user with the specified message and chat bubble style.
        /// </summary>
        public void Whisper(string recipient, string message, int bubble = 0)
            => Chat(ChatType.Whisper, $"{recipient} {message}", bubble);

        /// <summary>
        /// Talks with the specified message and chat bubble style.
        /// </summary>
        public void Talk(string message, int bubble = 0)
            => Chat(ChatType.Talk, message, bubble);

        /// <summary>
        /// Shouts with the specified message and chat bubble style.
        /// </summary>
        public void Shout(string message, int bubble = 0)
            => Chat(ChatType.Shout, message, bubble);
        #endregion

        #region - Room moderation -
        /// <summary>
        /// Mutes a user for the specified number of minutes.
        /// </summary>
        public void Mute(long userId, long roomId, int minutes) => Send(Out.RoomMuteUser, userId, roomId, minutes);

        /// <summary>
        /// Mutes a user for the specified number of minutes.
        /// </summary>
        public void Mute(IRoomUser user, int minutes) => Send(Out.RoomMuteUser, user.Id, RoomId, minutes);

        /// <summary>
        /// Kicks the specified user from the room.
        /// </summary>
        public void Kick(IRoomUser user) => Kick(user.Id);

        /// <summary>
        /// Kicks the specified user from the room.
        /// </summary>
        public void Kick(long userId) => Send(Out.KickUser, userId);

        /// <summary>
        /// Bans a user for the specified duration.
        /// </summary>
        public void Ban(long userId, long roomId, BanDuration duration) => Send(Out.RoomBanWithDuration, userId, roomId, duration.GetValue());

        /// <summary>
        /// Bans a user for the specified duration.
        /// </summary>
        public void Ban(IRoomUser user, BanDuration duration) => Send(Out.RoomBanWithDuration, user.Id, RoomId, duration.GetValue());

        /// <summary>
        /// Unbans a user from the specified room.
        /// </summary>
        public void Unban(long userId, long roomId) => Send(Out.RoomUnbanUser, userId, roomId);

        /// <summary>
        /// Unbans the specified user.
        /// </summary>
        public void Unban(IRoomUser user) => Unban(user.Id);

        /// <summary>
        /// Unbans the specified user.
        /// </summary>
        public void Unban(long userId) => Send(Out.RoomUnbanUser, userId, RoomId);

        /// <summary>
        /// Gives rights to the current room to the specified user.
        /// </summary>
        public void GiveRights(long userId) => Send(Out.AssignRights, userId);

        /// <summary>
        /// Removes rights to the current room from the specified users.
        /// </summary>
        public void RemoveRights(params long[] userIds) => Send(Out.RemoveRights, userIds);
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
        /// Places a sticky at the specified location.
        /// </summary>
        public void PlaceSticky(IInventoryItem item, WallLocation location)
        {
            if (item.Category != FurniCategory.Sticky)
                throw new InvalidOperationException("Item is not a sticky note");
            Send(Out.PlacePostIt, item.ItemId, location);
        }

        /// <summary>
        /// Places a sticky at the specified location.
        /// </summary>
        public void PlaceSticky(int itemId, WallLocation location) => Send(Out.PlacePostIt, itemId, location);

        /// <summary>
        /// Places a sticky at the specified location using a sticky pole.
        /// </summary>
        public void PlaceStickyWithPole(IInventoryItem item, WallLocation location, string color, string text)
            => PlaceStickyWithPole(item.ItemId, location, color, text);

        /// <summary>
        /// Places a sticky at the specified location using a sticky pole.
        /// </summary>
        public void PlaceStickyWithPole(long itemId, WallLocation location, string color, string text)
            => Send(Out.AddSpamWallPostIt, itemId, location, color, text);

        /// <summary>
        /// Gets the sticky data for the specified wall item.
        /// </summary>
        /// <param name="item">The sticky item to get data for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public Sticky GetSticky(IWallItem item, int timeout = DEFAULT_TIMEOUT) => GetSticky(item.Id, timeout);

        /// <summary>
        /// Gets the sticky data for the specified wall item.
        /// </summary>
        /// <param name="itemId">The item ID of the sticky to get data for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public Sticky GetSticky(long itemId, int timeout = DEFAULT_TIMEOUT)
            => new GetStickyTask(Interceptor, itemId).Execute(timeout, Ct);

        /// <summary>
        /// Updates the specified sticky.
        /// </summary>
        public void UpdateSticky(Sticky sticky) => UpdateSticky(sticky.Id, sticky.Color, sticky.Text);

        /// <summary>
        /// Updates the specified sticky.
        /// </summary>
        public void UpdateSticky(IWallItem item, string color, string text) => UpdateSticky(item.Id, color, text);

        /// <summary>
        /// Updates the specified sticky.
        /// </summary>
        public void UpdateSticky(long itemId, string color, string text) => Send(Out.SetStickyData, (LegacyLong)itemId, color, text);

        /// <summary>
        /// Deletes the specified sticky.
        /// </summary>
        public void DeleteSticky(Sticky sticky) => DeleteWallItem(sticky.Id);

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
        public void PlaceWallItem(long itemId, WallLocation location) => Send(Out.PlaceWallItem, (LegacyLong)itemId, location);

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

        #region - Trading -
        public bool IsTrading => _tradeManager.IsTrading;
        public bool IsTrader => _tradeManager.IsTrader;
        public bool HasAcceptedTrade => _tradeManager.HasAccepted;
        public bool HasPartnerAcceptedTrade => _tradeManager.HasPartnerAccepted;
        public bool IsTradeWaitingConfirmation => _tradeManager.IsWaitingConfirmation;
        public IRoomUser? TradePartner => _tradeManager.Partner;
        public ITradeOffer? OwnTradeOffer => _tradeManager.OwnOffer;
        public ITradeOffer? PartnerTradeOffer => _tradeManager.PartnerOffer;

        /// <summary>
        /// Trades the specified user.
        /// </summary>
        public void Trade(IRoomUser user) => Trade(user.Index);

        /// <summary>
        /// Trades the user with the specified index.
        /// </summary>
        public void Trade(int userIndex) => Send(Out.TradeOpen, userIndex);

        /// <summary>
        /// Offers the specified inventory item in the trade.
        /// </summary>
        public void Offer(IInventoryItem item) => Offer(item.ItemId);

        /// <summary>
        /// Offers the item with the specified item id in the trade.
        /// </summary>
        public void Offer(long itemId) => Send(Out.TradeAddItem, itemId);

        /// <summary>
        /// Offers the specified inventory items in the trade.
        /// </summary>
        public void Offer(IEnumerable<IInventoryItem> items) => Offer(items.Select(item => item.ItemId));

        /// <summary>
        /// Offers the items with the specified item ids in the trade.
        /// </summary>
        public void Offer(IEnumerable<long> itemIds) => Send(Out.TradeAddItems, itemIds);

        /// <summary>
        /// Cancels the offer for the specified item in the trade.
        /// </summary>
        public void CancelOffer(IInventoryItem item) => CancelOffer(item.ItemId);

        /// <summary>
        /// Cancels the offer for the item with the specified item id in the trade.
        /// </summary>
        public void CancelOffer(long itemId) => Send(Out.TradeRemoveItem, itemId);

        /// <summary>
        /// Cancels the trade.
        /// </summary>
        public void CancelTrade() => Send(Out.TradeClose);

        /// <summary>
        /// Accepts the trade.
        /// </summary>
        public void AcceptTrade() => Send(Out.TradeAccept);

        /// <summary>
        /// Confirms the trade.
        /// </summary>
        public void ConfirmTrade() => Send(Out.TradeConfirmAccept);
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

        /// <summary>
        /// Gets the inventory of the user. The user must be in a room for the server to return a response.
        /// </summary>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IInventory GetInventory(int timeout = DEFAULT_TIMEOUT)
            => new GetInventoryTask(Interceptor).Execute(timeout, Ct);
        #endregion

        #region - Navigator -
        /// <summary>
        /// Searches the navigator by category/filter and returns the list of navigator search results.
        /// </summary>
        /// <param name="category">The category to search.</param>
        /// <param name="filter">The filter text. Can be left empty.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public NavigatorSearchResults GetNav(string category, string filter = "", int timeout = DEFAULT_TIMEOUT)
            => new SearchNavigatorTask(Interceptor, category, filter).Execute(timeout, Ct);

        /// <summary>
        /// Searches the navigator by category/filter and returns a flattened view of <see cref="RoomInfo"/>.
        /// </summary>
        /// <param name="category">The category to search.</param>
        /// <param name="filter">The filter text. Can be left empty.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IEnumerable<IRoomInfo> SearchNav(string category, string filter = "", int timeout = DEFAULT_TIMEOUT)
            => GetNav(category, filter, timeout).GetRooms();

        /// <summary>
        /// Queries the navigator and returns a flattened view of <see cref="RoomInfo"/>.
        /// This is the same as searching by 'Anything' in the game client.
        /// </summary>
        /// <param name="query">The query text.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IEnumerable<IRoomInfo> QueryNav(string query, int timeout = DEFAULT_TIMEOUT)
            => GetNav("query", query, timeout).GetRooms();

        /// <summary>
        /// Searches the navigator by room name and returns a flattened view of <see cref="RoomInfo"/>.
        /// </summary>
        /// <param name="roomName">The room name to search for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        /// <returns></returns>
        public IEnumerable<IRoomInfo> SearchNavByName(string roomName, int timeout = DEFAULT_TIMEOUT)
            => GetNav("query", $"roomname:{roomName}", timeout).GetRooms();

        /// <summary>
        /// Searches the navigator by owner name and returns a flattened view of <see cref="RoomInfo"/>.
        /// </summary>
        /// <param name="ownerName">The room owner name to search for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IEnumerable<IRoomInfo> SearchNavByOwner(string ownerName, int timeout = DEFAULT_TIMEOUT)
            => GetNav("query", $"owner:{ownerName}", timeout).GetRooms();

        /// <summary>
        /// Searches the navigator by tag and returns a flattened view of <see cref="RoomInfo"/>.
        /// </summary>
        /// <param name="tag">The tag to search for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IEnumerable<IRoomInfo> SearchNavByTag(string tag, int timeout = DEFAULT_TIMEOUT)
            => GetNav("query", $"tag:{tag}", timeout).GetRooms();

        /// <summary>
        /// Searches the navigator by group name and returns a flattened view of <see cref="RoomInfo"/>.
        /// </summary>
        /// <param name="group">The group name to search for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IEnumerable<IRoomInfo> SearchNavByGroup(string group, int timeout = DEFAULT_TIMEOUT)
            => GetNav("query", $"group:{group}", timeout).GetRooms();
        #endregion

        #region - Catalog -
        public ICatalog GetCatalog(string mode = "NORMAL", int timeout = DEFAULT_TIMEOUT)
            => new GetCatalogTask(Interceptor, mode).Execute(timeout, Ct);

        public ICatalogPage GetCatalogPage(int pageId, string mode = "NORMAL", int timeout = DEFAULT_TIMEOUT)
            => new GetCatalogPageTask(Interceptor, pageId, mode).Execute(timeout, Ct);
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
        public void OnEnterQueue(Action<EventArgs> callback) => Register(_roomManager, nameof(_roomManager.EnteredQueue), callback);
        /// <summary>
        /// Registers a callback that is invoked when the user enters the room queue.
        /// </summary>
        public void OnEnterQueue(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EnteredQueue), callback);

        /// <summary>
        /// Registers a callback that is invoked when the user's queue position changes.
        /// </summary>
        public void OnUpdateQueue(Action<EventArgs> callback) => Register(_roomManager, nameof(_roomManager.QueuePositionUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when the user's queue position changes.
        /// </summary>
        public void OnUpdateQueue(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.QueuePositionUpdated), callback);

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
        public void OnEnterRoom(Action<EventArgs> callback) => Register(_roomManager, nameof(_roomManager.Entered), callback);
        /// <summary>
        /// Registers a callback that is invoked when the user has entered a room.
        /// </summary>
        public void OnEnterRoom(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.Entered), callback);

        /// <summary>
        /// Registers a callback that is invoked when the user has left room.
        /// </summary>
        public void OnLeaveRoom(Action<EventArgs> callback) => Register(_roomManager, nameof(_roomManager.Left), callback);
        /// <summary>
        /// Registers a callback that is invoked when the user has left room.
        /// </summary>
        public void OnLeaveRoom(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.Left), callback);

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
        public void OnRoomData(Action<RoomDataEventArgs> callback) => Register(_roomManager, nameof(_roomManager.RoomDataUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when the room data updates.
        /// </summary>
        public void OnRoomData(Func<EventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.RoomDataUpdated), callback);

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
        public void OnFloorItems(Action<FloorItemsEventArgs> callback) => Register(_roomManager, nameof(_roomManager.FloorItemsLoaded), callback);
        /// <summary>
        /// Registers a callback that is invoked when a room's floor items are first loaded.
        /// </summary>
        public void OnFloorItems(Func<FloorItemsEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.FloorItemsLoaded), callback);

        /// <summary>
        /// Registers a callback that is invoked when a floor item is placed in the room.
        /// </summary>
        public void OnAddFloorItem(Action<FloorItemEventArgs> callback) => Register(_roomManager, nameof(_roomManager.FloorItemAdded), callback);
        /// <summary>
        /// Registers a callback that is invoked when a floor item is placed in the room.
        /// </summary>
        public void OnAddFloorItem(Func<FloorItemEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.FloorItemAdded), callback);

        /// <summary>
        /// Registers a callback that is invoked when a floor item is updated.
        /// </summary>
        public void OnUpdateFloorItem(Action<FloorItemUpdatedEventArgs> callback) => Register(_roomManager, nameof(_roomManager.FloorItemUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when a floor item is updated.
        /// </summary>
        public void OnUpdateFloorItem(Func<FloorItemUpdatedEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.FloorItemUpdated), callback);

        /// <summary>
        /// Registers a callback that is invoked when a floor item's data is updated.
        /// </summary>
        public void OnUpdateFloorItemData(Action<FloorItemDataUpdatedEventArgs> callback) => Register(_roomManager, nameof(_roomManager.FloorItemDataUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when a floor item's data is updated.
        /// </summary>
        public void OnUpdateFloorItemData(Func<FloorItemDataUpdatedEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.FloorItemDataUpdated), callback);

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
        public void OnRemoveFloorItem(Action<FloorItemEventArgs> callback) => Register(_roomManager, nameof(_roomManager.FloorItemRemoved), callback);
        /// <summary>
        /// Registers a callback that is invoked when a floor item is removed from the room.
        /// </summary>
        public void OnRemoveFloorItem(Func<FloorItemEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.FloorItemRemoved), callback);

        /// <summary>
        /// Registers a callback that is invoked when a room's wall items are first loaded.
        /// </summary>
        public void OnWallItems(Action<WallItemsEventArgs> callback) => Register(_roomManager, nameof(_roomManager.WallItemsLoaded), callback);
        /// <summary>
        /// Registers a callback that is invoked when a room's wall items are first loaded.
        /// </summary>
        public void OnWallItems(Func<WallItemsEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.WallItemsLoaded), callback);

        /// <summary>
        /// Registers a callback that is invoked when a wall item is placed in the room.
        /// </summary>
        public void OnAddWallItem(Action<WallItemEventArgs> callback) => Register(_roomManager, nameof(_roomManager.WallItemAdded), callback);
        /// <summary>
        /// Registers a callback that is invoked when a wall item is placed in the room.
        /// </summary>
        public void OnAddWallItem(Func<WallItemEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.WallItemAdded), callback);

        /// <summary>
        /// Registers a callback that is invoked when a wall item is updated.
        /// </summary>
        public void OnUpdateWallItem(Action<WallItemUpdatedEventArgs> callback) => Register(_roomManager, nameof(_roomManager.WallItemUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when a wall item is updated.
        /// </summary>
        public void OnUpdateWallItem(Func<WallItemUpdatedEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.WallItemUpdated), callback);

        /// <summary>
        /// Registers a callback that is invoked when a wall item is removed from the room.
        /// </summary>
        public void OnRemoveWallItem(Action<WallItemEventArgs> callback) => Register(_roomManager, nameof(_roomManager.WallItemRemoved), callback);
        /// <summary>
        /// Registers a callback that is invoked when a wall item is removed from the room.
        /// </summary>
        public void OnRemoveWallItem(Func<WallItemEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.WallItemRemoved), callback);

        #endregion

        #region - Entity events -
        /// <summary>
        /// Registers a callback that is invoked when an entity is added to the room.
        /// </summary>
        /// <param name="callback"></param>
        public void OnAddEntity(Action<EntityEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityAdded), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity is added to the room.
        /// </summary>
        /// <param name="callback"></param>
        public void OnAddEntity(Func<EntityEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityAdded), callback);

        /// <summary>
        /// Registers a callback that is invoked when entities are added to the room.
        /// </summary>
        public void OnAddEntities(Action<EntitiesEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntitiesAdded), callback);
        /// <summary>
        /// Registers a callback that is invoked when entities are added to the room.
        /// </summary>
        public void OnAddEntities(Func<EntitiesEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntitiesAdded), callback);

        /// <summary>
        /// Registers a callback that is invoked when an entity is updated.
        /// </summary>
        public void OnUpdateEntity(Action<EntityEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity is updated.
        /// </summary>
        public void OnUpdateEntity(Func<EntityEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityUpdated), callback);

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
        public void OnUpdateUserData(Action<UserDataUpdatedEventArgs> callback) => Register(_roomManager, nameof(_roomManager.UserDataUpdated), callback);
        /// <summary>
        /// Registers a callback that is invoked when a user's figure, gender, motto or achievement score is updated.
        /// </summary>
        /// <param name="callback"></param>
        public void OnUpdateUserData(Func<UserDataUpdatedEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.UserDataUpdated), callback);

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
        public void OnRemoveEntity(Func<EntityEventArgs, Task> callback) => Register(_roomManager, nameof(_roomManager.EntityRemoved), callback);
        /// <summary>
        /// Registers a callback that is invoked when an entity is removed from the room.
        /// </summary>
        public void OnRemoveEntity(Action<EntityEventArgs> callback) => Register(_roomManager, nameof(_roomManager.EntityRemoved), callback);
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

        #region - Trade events -
        /// <summary>
        /// Registers a callback that is invoked when a trade is started.
        /// </summary>
        public void OnTradeStart(Action<TradeStartEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.Start), callback);
        /// <summary>
        /// Registers a callback that is invoked when a trade is started.
        /// </summary>
        public void OnTradeStart(Func<TradeStartEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.Start), callback);

        /// <summary>
        /// Registers a callback that is invoked when a trade fails to start.
        /// </summary>
        public void OnTradeStartFail(Action<TradeStartFailEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.StartFail), callback);
        /// <summary>
        /// Registers a callback that is invoked when a trade fails to start.
        /// </summary>
        public void OnTradeStartFail(Func<TradeStartFailEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.StartFail), callback);

        /// <summary>
        /// Registers a callback that is invoked when a trade is updated.
        /// </summary>
        public void OnTradeUpdate(Action<TradeOfferEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.Update), callback);
        /// <summary>
        /// Registers a callback that is invoked when a trade is updated.
        /// </summary>
        public void OnTradeUpdate(Func<TradeOfferEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.Update), callback);

        /// <summary>
        /// Registers a callback that is invoked when a user accepts the trade.
        /// </summary>
        public void OnTradeAccept(Action<TradeAcceptEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.Accept), callback);
        /// <summary>
        /// Registers a callback that is invoked when a user accepts the trade.
        /// </summary>
        public void OnTradeAccept(Func<TradeAcceptEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.Accept), callback);

        /// <summary>
        /// Registers a callback that is invoked when both users have accepted the trade and are waiting for each other's confirmation.
        /// </summary>
        public void OnTradeWaitingConfirm(Action<EventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.WaitingConfirm), callback);
        /// <summary>
        /// Registers a callback that is invoked when both users have accepted the trade and are waiting for each other's confirmation.
        /// </summary>
        public void OnTradeWaitingConfirm(Func<EventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.WaitingConfirm), callback);

        /// <summary>
        /// Registers a callback that is invoked when a trade is stopped.
        /// </summary>
        public void OnTradeStop(Action<TradeStopEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.Stop), callback);
        /// <summary>
        /// Registers a callback that is invoked when a trade is stopped.
        /// </summary>
        public void OnTradeStop(Func<TradeStopEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.Stop), callback);

        /// <summary>
        /// Registers a callback that is invoked when a trade is completed successfully.
        /// </summary>
        public void OnTradeComplete(Action<TradeCompleteEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.Complete), callback);
        /// <summary>
        /// Registers a callback that is invoked when a trade is completed successfully.
        /// </summary>
        public void OnTradeComplete(Func<TradeCompleteEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.Complete), callback);
        #endregion

        #endregion
    }
}
