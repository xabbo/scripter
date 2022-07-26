using System;
using System.Collections.Generic;
using System.Linq;

using Xabbo.Common;
using Xabbo.Messages;
using Xabbo.Interceptor;

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
    public void ToggleFloorItem(long itemId, int state) => Interceptor.Send(Out.UseStuff, itemId, state);

    /// <summary>
    /// Toggles the state of the specified wall item.
    /// </summary>
    public void ToggleWallItem(long itemId, int state) => Interceptor.Send(Out.UseWallItem, itemId, state);

    /// <summary>
    /// Uses the specified one-way gate.
    /// </summary>
    public void UseGate(long itemId) => Interceptor.Send(Out.EnterOneWayDoor, itemId);

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
    public void DeleteWallItem(long itemId) => Interceptor.Send(Out.RemoveItem, itemId);

    /// <summary>
    /// Places a floor item at the specified location.
    /// </summary>
    public void Place(IInventoryItem item, Point location, int dir = 0)
    {
        if (item.Type != ItemType.Floor)
            throw new InvalidOperationException("The specified item is not a floor item.");
        PlaceFloorItem(item.ItemId, location, dir);
    }

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
    public void Move(IFloorItem item, Point location, int dir = 0) => MoveFloorItem(item.Id, location, dir);

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
    public void PlaceFloorItem(long itemId, Point location, int dir = 0)
    {
        switch (CurrentClient)
        {
            case ClientType.Flash: Interceptor.Send(Out.PlaceRoomItem, $"{itemId} {location.X} {location.Y} {dir}"); break;
            case ClientType.Unity: Interceptor.Send(Out.PlaceRoomItem, itemId, location.X, location.Y, dir); break;
            default: throw new Exception("Unknown client protocol.");
        }
    }

    /// <summary>
    /// Moves a floor item to the specified location.
    /// </summary>
    public void MoveFloorItem(long itemId, Point location, int dir = 0) => Interceptor.Send(Out.MoveRoomItem, itemId, location.X, location.Y, dir);

    /// <summary>
    /// Picks up the specified floor item.
    /// </summary>
    public void PickupFloorItem(long itemId) => Interceptor.Send(Out.PickItemUpFromRoom, 2, itemId);

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
    public void MoveWallItem(long itemId, WallLocation location) => Interceptor.Send(Out.MoveWallItem, itemId, location);

    /// <summary>
    /// Moves a wall item to the specified location.
    /// </summary>
    public void MoveWallItem(long itemId, string location) => MoveWallItem(itemId, WallLocation.Parse(location));

    /// <summary>
    /// Picks up the specified wall item.
    /// </summary>
    public void PickupWallItem(long itemId) => Interceptor.Send(Out.PickItemUpFromRoom, 1, itemId);

    /// <summary>
    /// Updates the stack tile to the specified height.
    /// </summary>
    public void UpdateStackTile(IFloorItem stackTile, float height) => UpdateStackTile(stackTile.Id, height);

    /// <summary>
    /// Updates the stack tile to the specified height.
    /// </summary>
    public void UpdateStackTile(long stackTileId, float height)
        => Interceptor.Send(Out.StackingHelperSetCaretHeight, stackTileId, (int)Math.Round(height * 100.0));
}
