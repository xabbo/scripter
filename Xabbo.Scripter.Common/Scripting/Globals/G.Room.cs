using System;

using System.Collections.Generic;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.Core;
using Xabbo.Core.Game;
using Xabbo.Core.Tasks;

using Xabbo.Scripter.Tasks;
using Xabbo.Scripter.Runtime;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
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

    /// <summary>
    /// Gets if the user has permission to mute in the current room.
    /// </summary>
    public bool CanMute => _roomManager.CanMute;

    /// <summary>
    /// Gets if the user has permission to kick in the current room.
    /// </summary>
    public bool CanKick => _roomManager.CanKick;

    /// <summary>
    /// Gets if the user has permission to ban in the current room.
    /// </summary>
    public bool CanBan => _roomManager.CanBan;

    /// <summary>
    /// Gets if the user has permission to unban in the current room.
    /// This is an alias for <see cref="IsRoomOwner"/>.
    /// </summary>
    public bool CanUnban => IsRoomOwner;

    /// <summary>
    /// Gets if the user is the owner of the current room.
    /// </summary>
    public bool IsRoomOwner => _roomManager.IsOwner;

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
    public void SaveRoomSettings(RoomSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Interceptor.Send(Out.SaveRoomSettings, settings);
    }

    /// <summary>
    /// Modifies the settings of the specified room.
    /// </summary>
    public void ModifyRoomSettings(Action<RoomSettings> updater, long? roomId = null, int timeout = DEFAULT_TIMEOUT)
    {
        ArgumentNullException.ThrowIfNull(updater);
        RoomSettings settings = GetRoomSettings(roomId ?? RequireRoom().Id, timeout);
        updater(settings);
        SaveRoomSettings(settings);
    }

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
            RemoveAllRights(roomId)

            GetBannedUsers(roomId)

            Mood light control

            GetRoomWordFilter()
            SetRoomBackground(...)

            MuteRoom(...)

            SearchRooms(text)
            SearchRoomsByTag(text)

            GetRoomsWithFriends()
            GetFriendsRooms()
            GetRoomsInGroup()
            GetRoomHistory()
            GetFavoriteRooms()
            GetRoomsWithRights()

            SetHomeRoom()
    */
}
