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
using Xabbo.Common;

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

        /// <summary>
        /// Gets the interceptor service.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IInterceptor Interceptor => _scriptHost.Interceptor;

        /// <summary>
        /// Gets the currently connected client type.
        /// </summary>
        public ClientType CurrentClient => Interceptor.Client;

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

        #region - Net -
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
        /// <returns>The first packet captured with a header that matches one of the specified headers.</returns>
        public IReadOnlyPacket Receive(HeaderSet headers, int timeout = -1, bool block = false)
            => ReceiveAsync(headers, timeout, block).GetAwaiter().GetResult();

        /// <summary>
        /// Captures a packet with a header that matches any of the specified headers.
        /// </summary>
        /// <param name="tuple">The message headers to listen for.</param>
        /// <param name="timeout">The time to wait for a packet to be captured.</param>
        /// <param name="block">Whether to block the captured packet.</param>
        /// <returns>The first packet captured with a header that matches one of the specified headers.</returns>
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
                _dispatcher.AddIntercept(header, callback, CurrentClient);
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
        public void OnIntercept(ITuple headers, Func<InterceptArgs, Task> callback)
            => OnIntercept(HeaderSet.FromTuple(headers), callback);

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
        /// <summary>
        /// Shows a client-side chat bubble.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="index">
        /// The index of the entity to display the chat bubble from.
        /// If set to <c>-1</c>, this will attempt to use the user's own index.
        /// </param>
        /// <param name="bubble">The bubble style.</param>
        /// <param name="type">The type of chat bubble to display.</param>
        public void ShowBubble(string message, int index = -1, int bubble = 30, ChatType type = ChatType.Whisper)
        {
            if (index == -1) index = Self?.Index ?? -1;

            Interceptor.Send(In.Whisper, index, message, 0, bubble, 0, 0);
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
        /// <param name="motto">The new motto.</param>
        public void SetMotto(string motto) => Interceptor.Send(Out.ChangeAvatarMotto, motto);

        /// <summary>
        /// Sets the user's figure.
        /// </summary>
        /// <param name="figureString">The figure string.</param>
        /// <param name="gender">The gender of the figure.</param>
        public void SetFigure(string figureString, Gender gender)
            => Interceptor.Send(Out.UpdateAvatar, gender.ToShortString(), figureString);

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
        /// Gets the user's badges.
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
            Interceptor.Send(Out.GetGuildMemberships);
            var packet = receiveTask.GetAwaiter().GetResult();

            var list = new List<GroupInfo>();
            int n = packet.ReadInt();
            for (int i = 0; i < n; i++)
                list.Add(GroupInfo.Parse(packet));

            return list;
        }

        /// <summary>
        /// Gets the user's achievements.
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
        /// Gets the data of the specified room.
        /// </summary>
        /// <param name="roomId">The ID of the room.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IRoomData GetRoomData(long roomId, int timeout = DEFAULT_TIMEOUT)
            => new GetRoomDataTask(Interceptor, roomId).Execute(timeout, Ct);

        /// <summary>
        /// Sends a request to create a room with the specified parameters.
        /// </summary>
        public void CreateRoom(string name, string description, string model,
            RoomCategory category = RoomCategory.PersonalSpace,
            int maxUsers = 50,
            TradePermissions trading = TradePermissions.NotAllowed)
        {
            Interceptor.Send(
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
        public RoomSettings GetRoomSettings(long roomId, int timeout = DEFAULT_TIMEOUT) =>
            new GetRoomSettingsTask(Interceptor, roomId).Execute(timeout, Ct);

        /// <summary>
        /// Saves the specified room settings.
        /// </summary>
        public void SaveRoomSettings(RoomSettings settings) => Interceptor.Send(Out.SaveRoomSettings, settings);

        /// <summary>
        /// Sends a request to delete a room with the specified ID.
        /// </summary>
        public void DeleteRoom(long roomId) => Interceptor.Send(Out.DeleteFlat, (LegacyLong)roomId);

        /// <summary>
        /// Attempts to enter the specified room.
        /// </summary>
        public RoomEntryResult EnsureEnterRoom(long roomId, string password = "", int timeout = DEFAULT_TIMEOUT)
            => new EnterRoomTask(Interceptor, roomId, password).Execute(timeout, Ct);

        /// <summary>
        /// Sends a request to enter the specified room.
        /// </summary>
        public void EnterRoom(long roomId, string password = "") => Interceptor.Send(Out.FlatOpc, (LegacyLong)roomId, password, 0, -1, -1);

        /// <summary>
        /// Sends a request to leave the room.
        /// </summary>
        public void LeaveRoom() => Interceptor.Send(Out.Quit);

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
            switch (furni.Type)
            {
                case ItemType.Floor: UseFloorItem(furni.Id); break;
                case ItemType.Wall: UseWallItem(furni.Id); break;
                default: throw new Exception($"Unknown item type: {furni.Type}.");
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
            switch (furni.Type)
            {
                case ItemType.Floor: ToggleFloorItem(furni.Id, state); break;
                case ItemType.Wall: ToggleWallItem(furni.Id, state); break;
                default: throw new Exception($"Unknown item type: {furni.Type}.");
            }
        }

        /// <summary>
        /// Toggles the state of the specified floor item.
        /// </summary>
        public void ToggleFloorItem(long itemId, int state) => Interceptor.Send(Out.UseStuff, (LegacyLong)itemId, state);

        /// <summary>
        /// Toggles the state of the specified wall item.
        /// </summary>
        public void ToggleWallItem(long itemId, int state) => Interceptor.Send(Out.UseWallItem, (LegacyLong)itemId, state);

        /// <summary>
        /// Uses the specified one-way gate.
        /// </summary>
        public void UseGate(long itemId) => Interceptor.Send(Out.EnterOneWayDoor, (LegacyLong)itemId);

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
        public void DeleteWallItem(long itemId) => Interceptor.Send(Out.RemoveItem, (LegacyLong)itemId);

        /// <summary>
        /// Places a floor item at the specified location.
        /// </summary>
        public void Place(IInventoryItem item, int x, int y, int dir = 0)
        {
            if (item.Type != ItemType.Floor)
                throw new InvalidOperationException("The specified item is not a floor item.");
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
                throw new InvalidOperationException("The specified item is not a wall item.");
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
            switch (furni.Type)
            {
                case ItemType.Floor: PickupFloorItem(furni.Id); break;
                case ItemType.Wall: PickupWallItem(furni.Id); break;
                default: throw new Exception($"Unknown item type: {furni.Type}.");
            }
        }

        /// <summary>
        /// Places a floor item at the specified location.
        /// </summary>
        public void PlaceFloorItem(long itemId, int x, int y, int dir = 0)
        {
            switch (CurrentClient)
            {
                case ClientType.Flash: Interceptor.Send(Out.PlaceRoomItem, $"{itemId} {x} {y} {dir}"); break;
                case ClientType.Unity: Interceptor.Send(Out.PlaceRoomItem, itemId, x, y, dir); break;
                default: throw new Exception("Unknown client protocol.");
            }
        }

        /// <summary>
        /// Moves a floor item to the specified location.
        /// </summary>
        public void MoveFloorItem(long itemId, int x, int y, int dir = 0) => Interceptor.Send(Out.MoveRoomItem, (LegacyLong)itemId, x, y, dir);

        /// <summary>
        /// Picks up the specified floor item.
        /// </summary>
        public void PickupFloorItem(long itemId) => Interceptor.Send(Out.PickItemUpFromRoom, 2, (LegacyLong)itemId);

        /// <summary>
        /// Places a wall item at the specified location.
        /// </summary>
        public void PlaceWallItem(long itemId, WallLocation location)
        {
            switch (CurrentClient)
            {
                case ClientType.Flash: Interceptor.Send(Out.PlaceRoomItem, $"{itemId} {location}"); break;
                case ClientType.Unity: Interceptor.Send(Out.PlaceWallItem, itemId, location); break;
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
        public void MoveWallItem(long itemId, WallLocation location) => Interceptor.Send(Out.MoveWallItem, (LegacyLong)itemId, location);

        /// <summary>
        /// Moves a wall item to the specified location.
        /// </summary>
        public void MoveWallItem(long itemId, string location) => MoveWallItem(itemId, WallLocation.Parse(location));

        /// <summary>
        /// Picks up the specified wall item.
        /// </summary>
        public void PickupWallItem(long itemId) => Interceptor.Send(Out.PickItemUpFromRoom, 1, (LegacyLong)itemId);

        /// <summary>
        /// Updates the stack tile to the specified height.
        /// </summary>
        public void UpdateStackTile(IFloorItem stackTile, double height) => UpdateStackTile(stackTile.Id, height);
        /// <summary>
        /// Updates the stack tile to the specified height.
        /// </summary>
        public void UpdateStackTile(long stackTileId, double height)
            => Interceptor.Send(Out.StackingHelperSetCaretHeight, (LegacyLong)stackTileId, (int)Math.Round(height * 100.0));
        #endregion

        #region - Entity interaction -
        /// <summary>
        /// Ignores the specified user.
        /// </summary>
        public void Ignore(IRoomUser user) => Ignore(user.Name);

        /// <summary>
        /// Ignores the specified user.
        /// </summary>
        public void Ignore(string name) => Interceptor.Send(Out.IgnoreUser, name);

        /// <summary>
        /// Unignores the specified user.
        /// </summary>
        public void Unignore(IRoomUser user) => Unignore(user.Name);

        /// <summary>
        /// Unignores the specified user.
        /// </summary>
        public void Unignore(string name) => Interceptor.Send(Out.UnignoreUser, name);

        /// <summary>
        /// Sends a friend request to the specified user.
        /// </summary>
        public void FriendRequest(IRoomUser user) => FriendRequest(user.Name);

        /// <summary>
        /// Sends a friend request to the specified user.
        /// </summary>
        public void FriendRequest(string name) => Interceptor.Send(Out.RequestFriend, name);

        /// <summary>
        /// Respects the specified user.
        /// </summary>
        public void Respect(long userId) => Interceptor.Send(Out.RespectUser, (LegacyLong)userId);

        /// <summary>
        /// Respects the specified user.
        /// </summary>
        public void Respect(IRoomUser user) => Respect(user.Id);

        /// <summary>
        /// Scratches (or treats) the specified pet.
        /// </summary>
        public void Scratch(long petId) => Interceptor.Send(Out.RespectPet, (LegacyLong)petId);

        /// <summary>
        /// Scratches (or treats) the specified pet.
        /// </summary>
        public void Scratch(IPet pet) => Scratch(pet.Id);

        /// <summary>
        /// Mounts or dismounts the pet with the specified id.
        /// </summary>
        /// <param name="petId">The id of the pet to (dis)mount.</param>
        /// <param name="mount">Whether to mount or dismount.</param>
        public void Ride(long petId, bool mount) => Interceptor.Send(Out.MountPet, (LegacyLong)petId, mount);

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
        public void JoinGroup(long groupId) => Interceptor.Send(Out.JoinHabboGroup, (LegacyLong)groupId);

        /// <summary>
        /// Leaves the group with the specified ID.
        /// </summary>
        public void LeaveGroup(long groupId) => Interceptor.Send(Out.KickMember, (LegacyLong)groupId, (LegacyLong)UserId, false);

        /// <summary>
        /// Sets the specified group as the user's favourite group.
        /// </summary>
        /// <param name="groupId">The ID of the group to set as the user's favourite.</param>
        public void SetGroupFavourite(long groupId) => Interceptor.Send(Out.SelectFavouriteHabboGroup, groupId);

        /// <summary>
        /// Unsets the specified group as the user's favourite group.
        /// </summary>
        /// <param name="groupId">The ID of the group to remove from the user's favourite.</param>
        public void RemoveGroupFavourite(long groupId) => Interceptor.Send(Out.DeselectFavouriteHabboGroup, groupId);

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

        /// <summary>
        /// Accepts a user into the specified group.
        /// </summary>
        public void AcceptGroupMember(long groupId, long userId) => Interceptor.Send(Out.ApproveMembershipRequest, (LegacyLong)groupId, (LegacyLong)userId);

        /// <summary>
        /// Rejects a user from joining the specified group.
        /// </summary>
        public void RejectGroupMember(long groupId, long userId) => Interceptor.Send(Out.RejectMembershipRequest, (LegacyLong)groupId, (LegacyLong)userId);

        /// <summary>
        /// Kicks a user from the specified group.
        /// </summary>
        public void KickGroupMember(long groupId, long userId) => Interceptor.Send(Out.KickMember, (LegacyLong)groupId, (LegacyLong)userId, false);
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

        /// <summary>
        /// Searches for the specified user by name and returns the matching search result if it exists.
        /// </summary>
        /// <param name="name">The name of the user to search for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public UserSearchResult? SearchUser(string name, int timeout = DEFAULT_TIMEOUT)
            => new SearchUserTask(Interceptor, name).Execute(timeout, Ct).GetResult(name);

        /// <summary>
        /// Searches for users by name and returns the results.
        /// </summary>
        /// <param name="name">The name of the user to search for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public UserSearchResults SearchUsers(string name, int timeout = DEFAULT_TIMEOUT)
            => new SearchUserTask(Interceptor, name).Execute(timeout, Ct);

        /// <summary>
        /// Gets the profile of the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IUserProfile GetProfile(long userId, int timeout = DEFAULT_TIMEOUT)
            => new GetProfileTask(Interceptor, userId).Execute(timeout, Ct);
        #endregion
    }
}
