using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xabbo.Core;
using Xabbo.Messages;

namespace Xabbo.Scripter.Scripting
{
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
        public bool IsFriend(IRoomUser user) => _friendManager.IsFriend(user.Id);

        /// <summary>
        /// Accepts a friend request from the specified user.
        /// </summary>
        public void AcceptFriendRequest(long userId) => AcceptFriendRequests(userId);

        /// <summary>
        /// Accepts friend requests from the specified users.
        /// </summary>
        public void AcceptFriendRequests(IEnumerable<long> userIds) => Send(Out.AcceptFriend, userIds.Cast<LegacyLong>());

        /// <summary>
        /// Accepts friend requests from the specified users.
        /// </summary>
        public void AcceptFriendRequests(params long[] userIds) => AcceptFriendRequests((IEnumerable<long>)userIds);

        /// <summary>
        /// Declines friend requests from the specified users.
        /// </summary>
        public void DeclineFriendRequests(IEnumerable<long> userIds) => Send(Out.DeclineFriend, false, userIds.Cast<LegacyLong>());

        /// <summary>
        /// Declines friend requests from the specified users.
        /// </summary>
        public void DeclineFriendRequests(params long[] userIds) => DeclineFriendRequests((IEnumerable<long>)userIds);

        /// <summary>
        /// Declines a friend request from the specified user.
        /// </summary>
        public void DeclineFriendRequest(long userId) => DeclineFriendRequests(userId);

        /// <summary>
        /// Declines all incoming friend requests.
        /// </summary>
        public void DeclineAllFriendRequests() => Send(Out.DeclineFriend, true, 0);

        /// <summary>
        /// Sends a friend request to the specified user.
        /// </summary>
        public void AddFriend(string name) => Send(Out.RequestFriend, name);

        /// <summary>
        /// Sends a friend request to the specified user.
        /// </summary>
        public void AddFriend(IRoomUser user) => AddFriend(user.Name);

        /// <summary>
        /// Removes the specified user from the user's friend list.
        /// </summary>
        public void RemoveFriend(long userId) => RemoveFriends(userId);

        /// <summary>
        /// Removes the specified users from the user's friend list.
        /// </summary>
        public void RemoveFriends(params long[] userIds) => Send(Out.RemoveFriend, userIds);

        /// <summary>
        /// Sends a private message to a friend with the specified ID.
        /// </summary>
        public void SendMessage(long userId, string message) => Send(Out.SendMessage, userId, message);
    }
}
