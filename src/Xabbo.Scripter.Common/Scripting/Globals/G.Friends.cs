using System;
using System.Collections.Generic;
using System.Linq;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.Core;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
    /// <summary>
    /// Gets the user's friends.
    /// </summary>
    public IEnumerable<IFriend> Friends => _friendManager.Friends;

    /// <summary>
    /// Gets if the user with the specified ID is a friend.
    /// </summary>
    public bool IsFriend(long id) => _friendManager.IsFriend(id);

    /// <summary>
    /// Gets if the user with the specified name is a friend.
    /// </summary>
    public bool IsFriend(string name) => _friendManager.IsFriend(name);

    /// <summary>
    /// Gets if the specified <see cref="IRoomUser"/> is a friend.
    /// </summary>
    public bool IsFriend(IRoomUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return _friendManager.IsFriend(user.Id);
    }

    /// <summary>
    /// Accepts a friend request from the specified user.
    /// </summary>
    public void AcceptFriendRequest(long userId) => AcceptFriendRequests(new[] { userId });

    /// <summary>
    /// Accepts friend requests from the specified users.
    /// </summary>
    public void AcceptFriendRequests(IEnumerable<long> userIds) => Interceptor.Send(Out.AcceptFriend, userIds);

    /// <summary>
    /// Declines a friend request from the specified user.
    /// </summary>
    public void DeclineFriendRequest(long userId) => DeclineFriendRequests(new[] { userId });

    /// <summary>
    /// Declines friend requests from the specified users.
    /// </summary>
    public void DeclineFriendRequests(IEnumerable<long> userIds) => Interceptor.Send(Out.DeclineFriend, false, userIds);

    /// <summary>
    /// Declines all incoming friend requests.
    /// </summary>
    public void DeclineAllFriendRequests() => Interceptor.Send(Out.DeclineFriend, true, 0);

    /// <summary>
    /// Sends a friend request to the specified user.
    /// </summary>
    public void AddFriend(string name) => Interceptor.Send(Out.RequestFriend, name);

    /// <summary>
    /// Sends a friend request to the specified user.
    /// </summary>
    public void AddFriend(IRoomUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        AddFriend(user.Name);
    }

    /// <summary>
    /// Removes the specified user from the user's friend list.
    /// </summary>
    public void RemoveFriend(long userId) => RemoveFriends(new[] { userId });

    /// <summary>
    /// Removes the specified friend.
    /// </summary>
    public void RemoveFriend(IFriend friend)
    {
        ArgumentNullException.ThrowIfNull(friend);
        RemoveFriends(new[] { friend.Id });
    }

    /// <summary>
    /// Removes the specified users from the user's friend list.
    /// </summary>
    public void RemoveFriends(IEnumerable<long> userIds) => Interceptor.Send(Out.RemoveFriend, userIds);

    /// <summary>
    /// Removes the specified users from the user's friend list.
    /// </summary>
    public void RemoveFriends(IEnumerable<IFriend> friends) => Interceptor.Send(Out.RemoveFriend, friends.Where(x => x is not null).Select(x => x.Id));

    /// <summary>
    /// Sends a private message to a friend with the specified ID.
    /// </summary>
    public void SendMessage(long userId, string message) => Interceptor.Send(Out.SendMessage, userId, message);

    /// <summary>
    /// Sends a private message to the specified friend.
    /// </summary>
    public void SendMessage(IFriend friend, string message)
    {
        ArgumentNullException.ThrowIfNull(friend);
        SendMessage(friend.Id, message);
    }
}
