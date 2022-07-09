using System;
using System.Linq;

using GalaSoft.MvvmLight;
using Xabbo.Core.Events;
using Xabbo.Core.Game;
using Xabbo.Interceptor;

using Xabbo.Scripter.Services;

namespace Xabbo.Scripter.ViewModel
{
    public class StatusBarViewManager : ObservableObject
    {
        public IRemoteInterceptor Interceptor { get; }

        private readonly IGameManager _gameManager;

        protected IRoom? Room => _gameManager.RoomManager.Room;

        private bool _showUsername = true;
        public bool ShowUsername
        {
            get => _showUsername;
            set => Set(ref _showUsername, value);
        }

        #region - Remote state -

        private bool isRemoteConnected;
        public bool IsRemoteConnected
        {
            get => isRemoteConnected;
            set => Set(ref isRemoteConnected, value);
        }

        private bool _isGameConnected;
        public bool IsGameConnected
        {
            get => _isGameConnected;
            set => Set(ref _isGameConnected, value);
        }

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value);
        }
        #endregion

        #region - Game state -
        private bool _hasUserData;
        public bool HasUserData
        {
            get => _hasUserData;
            set => Set(ref _hasUserData, value);
        }

        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set => Set(ref _userName, value);
        }

        private bool _beenInRoom;
        public bool BeenInRoom
        {
            get => _beenInRoom;
            set => Set(ref _beenInRoom, value);
        }

        private bool _isInRoom;
        public bool IsInRoom
        {
            get => _isInRoom;
            set => Set(ref _isInRoom, value);
        }

        private string _currentRoomName = string.Empty;
        public string CurrentRoomName
        {
            get => _currentRoomName;
            set => Set(ref _currentRoomName, value);
        }

        private int _userCount;
        public int UserCount
        {
            get => _userCount;
            set => Set(ref _userCount, value);
        }

        private int _botCount;
        public int BotCount
        {
            get => _botCount;
            set => Set(ref _botCount, value);
        }

        private int _petCount;
        public int PetCount
        {
            get => _petCount;
            set => Set(ref _petCount, value);
        }

        private int _furniCount;
        public int FurniCount
        {
            get => _furniCount;
            set => Set(ref _furniCount, value);
        }

        private int _floorItemCount;
        public int FloorItemCount
        {
            get => _floorItemCount;
            set => Set(ref _floorItemCount, value);
        }

        private int _wallItemCount;
        public int WallItemCount
        {
            get => _wallItemCount;
            set => Set(ref _wallItemCount, value);
        }

        #endregion

        public StatusBarViewManager(IRemoteInterceptor interceptor,
            IGameManager gameManager)
        {
            Interceptor = interceptor;
            Interceptor.InterceptorConnected += OnInterceptorConnected;
            Interceptor.Initialized += OnInterceptorInitialized;
            Interceptor.Connected += OnConnectionStart;
            Interceptor.Disconnected += OnConnectionEnd;
            Interceptor.InterceptorDisconnected += OnInterceptorDisconnected;

            _gameManager = gameManager;
            _gameManager.ProfileManager.LoadedUserData += OnLoadedUserData;

            _gameManager.RoomManager.Entered += OnEnteredRoom;
            _gameManager.RoomManager.RoomDataUpdated += OnRoomDataUpdated;
            _gameManager.RoomManager.Left += OnLeftRoom;
            _gameManager.RoomManager.EntitiesAdded += OnEntitiesAdded;
            _gameManager.RoomManager.EntityRemoved += OnEntityRemoved;
            _gameManager.RoomManager.FloorItemAdded += OnFloorItemAdded;
            _gameManager.RoomManager.FloorItemRemoved += OnFloorItemRemoved;
            _gameManager.RoomManager.WallItemAdded += OnWallItemAdded;
            _gameManager.RoomManager.WallItemRemoved += OnWallItemRemoved;
        }

        private void OnLoadedUserData(object? sender, EventArgs e)
        {
            HasUserData = true;
            UserName = _gameManager.ProfileManager.UserData?.Name ?? "unknown";
        }

        private void OnFloorItemAdded(object? sender, Xabbo.Core.Events.FloorItemEventArgs e) => UpdateFurniCount();
        private void OnFloorItemRemoved(object? sender, Xabbo.Core.Events.FloorItemEventArgs e) => UpdateFurniCount();
        private void OnWallItemAdded(object? sender, Xabbo.Core.Events.WallItemEventArgs e) => UpdateFurniCount();
        private void OnWallItemRemoved(object? sender, Xabbo.Core.Events.WallItemEventArgs e) => UpdateFurniCount();

        private void UpdateEntityCount()
        {
            UserCount = Room?.Users.Count() ?? 0;
            BotCount = Room?.Bots.Count() ?? 0;
            PetCount = Room?.Pets.Count() ?? 0;
        }

        private void UpdateFurniCount()
        {
            FurniCount = Room?.Furni.Count() ?? 0;
            FloorItemCount = Room?.FloorItems.Count() ?? 0;
            WallItemCount = Room?.WallItems.Count() ?? 0;
        }

        private void OnEnteredRoom(object? sender, RoomEventArgs e)
        {
            UpdateEntityCount();
            UpdateFurniCount();
            IsInRoom = BeenInRoom = true;
            CurrentRoomName = e.Room.Data?.Name ?? string.Empty;
        }

        private void OnRoomDataUpdated(object? sender, RoomDataEventArgs e)
        {
            CurrentRoomName = e.Data.Name;
        }

        private void OnLeftRoom(object? sender, EventArgs e)
        {
            IsInRoom = false;
            CurrentRoomName = string.Empty;
        }

        private void OnEntityRemoved(object? sender, EntityEventArgs e) => UpdateEntityCount();
        private void OnEntitiesAdded(object? sender, EntitiesEventArgs e) => UpdateEntityCount();

        private void ResetState()
        {
            HasUserData =
            BeenInRoom =
            IsInRoom = false;
            CurrentRoomName = string.Empty;
            FurniCount =
            FloorItemCount =
            WallItemCount =
            UserCount =
            BotCount =
            PetCount = 0;
        }

        private void OnInterceptorConnected(object? sender, EventArgs e)
        {
            IsRemoteConnected = true;
            IsGameConnected = false;
        }

        private void OnInterceptorInitialized(object? sender, EventArgs e)
        {

        }

        private void OnConnectionStart(object? sender, EventArgs e)
        {
            ResetState();

            IsGameConnected = true;
        }

        private void OnConnectionEnd(object? sender, EventArgs e)
        {
            IsGameConnected = false;
        }

        private void OnInterceptorDisconnected(object? sender, EventArgs e)
        {
            IsRemoteConnected = false;
            IsGameConnected = false;
        }
    }
}
