using System;
using System.Linq;

using Xabbo.Core;
using Xabbo.Messages;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Mutes a user for the specified number of minutes.
        /// </summary>
        public void Mute(long userId, long roomId, int minutes) => Send(Out.RoomMuteUser, (LegacyLong)userId, (LegacyLong)roomId, minutes);

        /// <summary>
        /// Mutes a user for the specified number of minutes.
        /// </summary>
        public void Mute(IRoomUser user, int minutes) => Mute(user.Id, RoomId, minutes);

        /// <summary>
        /// Kicks the specified user from the room.
        /// </summary>
        public void Kick(long userId) => Send(Out.KickUser, (LegacyLong)userId);

        /// <summary>
        /// Kicks the specified user from the room.
        /// </summary>
        public void Kick(IRoomUser user) => Kick(user.Id);

        /// <summary>
        /// Bans a user for the specified duration.
        /// </summary>
        public void Ban(long userId, long roomId, BanDuration duration)
            => Send(Out.RoomBanWithDuration, (LegacyLong)userId, (LegacyLong)roomId, duration.GetValue());

        /// <summary>
        /// Bans a user for the specified duration.
        /// </summary>
        public void Ban(IRoomUser user, BanDuration duration) => Ban(user.Id, RoomId, duration);

        /// <summary>
        /// Unbans a user from the specified room.
        /// </summary>
        public void Unban(long userId, long roomId) => Send(Out.RoomUnbanUser, (LegacyLong)userId, (LegacyLong)roomId);

        /// <summary>
        /// Unbans the specified user.
        /// </summary>
        public void Unban(IRoomUser user) => Unban(user.Id);

        /// <summary>
        /// Unbans the specified user.
        /// </summary>
        public void Unban(long userId) => Send(Out.RoomUnbanUser, (LegacyLong)userId, (LegacyLong)RoomId);

        /// <summary>
        /// Gives rights to the current room to the specified user.
        /// </summary>
        public void GiveRights(long userId) => Send(Out.AssignRights, (LegacyLong)userId);

        /// <summary>
        /// Removes rights to the current room from the specified users.
        /// </summary>
        public void RemoveRights(params long[] userIds) => Send(Out.RemoveRights, userIds.Cast<LegacyLong>());
    }
}
