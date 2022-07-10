using System;

using Xabbo.Core;
using Xabbo.Core.Game;

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
}
