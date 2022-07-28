using System;
using System.Reflection;
using System.Threading.Tasks;

using Xabbo.Core.Events;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
    /// <summary>
    /// Registers a callback to an event on the specified source.
    /// The callback is deregistered when the globals are disposed.
    /// </summary>
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

    /// <summary>
    /// Registers a callback to an event on the specified source.
    /// The callback is deregistered when the globals are disposed.
    /// </summary>
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
}
