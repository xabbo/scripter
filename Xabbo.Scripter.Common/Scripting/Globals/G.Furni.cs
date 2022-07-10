using System;
using System.Collections.Generic;
using System.Linq;

using Xabbo.Core;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
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
}
