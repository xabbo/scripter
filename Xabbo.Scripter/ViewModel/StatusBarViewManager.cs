using System;
using System.Linq;

using GalaSoft.MvvmLight;

using Xabbo.Interceptor;

using Xabbo.Scripter.Services;

namespace Xabbo.Scripter.ViewModel
{
    public class StatusBarViewManager : ObservableObject
    {
        private readonly IRemoteInterceptor _interceptor;
        private readonly IGameManager _gameManager;

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

        private int _userCount = -1;
        public int UserCount
        {
            get => _userCount;
            set => Set(ref _userCount, value);
        }

        private int _botCount = -1;
        public int BotCount
        {
            get => _botCount;
            set => Set(ref _botCount, value);
        }

        private int _petCount = -1;
        public int PetCount
        {
            get => _petCount;
            set => Set(ref _petCount, value);
        }

        private int _furniCount = -1;
        public int FurniCount
        {
            get => _furniCount;
            set => Set(ref _furniCount, value);
        }
        #endregion

        public StatusBarViewManager(IRemoteInterceptor interceptor,
            IGameManager gameManager)
        {
            _interceptor = interceptor;
            _interceptor.InterceptorConnected += OnInterceptorConnected;
            _interceptor.Initialized += OnInterceptorInitialized;
            _interceptor.Connected += OnConnectionStart;
            _interceptor.Disconnected += OnConnectionEnd;
            _interceptor.InterceptorDisconnected += OnInterceptorDisconnected;

            _gameManager = gameManager;
            _gameManager.ProfileManager.LoadedUserData += OnLoadedUserData;

            _gameManager.RoomManager.Entered += OnEnteredRoom;
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
        }

        private void OnFloorItemAdded(object? sender, Xabbo.Core.Events.FloorItemEventArgs e)
            => UpdateFurniCount();
        private void OnFloorItemRemoved(object? sender, Xabbo.Core.Events.FloorItemEventArgs e)
            => UpdateFurniCount();
        private void OnWallItemAdded(object? sender, Xabbo.Core.Events.WallItemEventArgs e)
            => UpdateFurniCount();
        private void OnWallItemRemoved(object? sender, Xabbo.Core.Events.WallItemEventArgs e)
            => UpdateFurniCount();

        private void UpdateEntityCount()
        {
            UserCount = _gameManager.RoomManager.Room?.Users.Count() ?? 0;
            PetCount = _gameManager.RoomManager.Room?.Pets.Count() ?? 0;
        }

        private void UpdateFurniCount()
        {
            FurniCount = _gameManager.RoomManager.Room?.Furni.Count() ?? 0;
        }

        private void OnEnteredRoom(object? sender, Xabbo.Core.Events.RoomEventArgs e)
        {
            UpdateEntityCount();
            UpdateFurniCount();
            IsInRoom = true;
        }

        private void OnLeftRoom(object? sender, EventArgs e)
        {
            IsInRoom = false;
        }

        private void OnEntityRemoved(object? sender, Xabbo.Core.Events.EntityEventArgs e)
        {
            UpdateEntityCount();
        }

        private void OnEntitiesAdded(object? sender, Xabbo.Core.Events.EntitiesEventArgs e)
        {
            UpdateEntityCount();
        }


        private void ResetState()
        {
            HasUserData =
            BeenInRoom =
            IsInRoom = false;
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
