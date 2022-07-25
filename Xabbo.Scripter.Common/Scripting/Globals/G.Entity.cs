using System;
using System.Collections.Generic;
using System.Linq;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.Core;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
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
    public void Respect(long userId) => Interceptor.Send(Out.RespectUser, userId);

    /// <summary>
    /// Respects the specified user.
    /// </summary>
    public void Respect(IRoomUser user) => Respect(user.Id);

    /// <summary>
    /// Scratches (or treats) the specified pet.
    /// </summary>
    public void Scratch(long petId) => Interceptor.Send(Out.RespectPet, petId);

    /// <summary>
    /// Scratches (or treats) the specified pet.
    /// </summary>
    public void Scratch(IPet pet) => Scratch(pet.Id);

    /// <summary>
    /// Mounts or dismounts the pet with the specified id.
    /// </summary>
    /// <param name="petId">The id of the pet to (dis)mount.</param>
    /// <param name="mount">Whether to mount or dismount.</param>
    public void Ride(long petId, bool mount) => Interceptor.Send(Out.MountPet, petId, mount);

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
}
