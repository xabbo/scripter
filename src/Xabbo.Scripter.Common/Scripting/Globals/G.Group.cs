using System;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.Core;
using Xabbo.Core.Tasks;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
    /// <summary>
    /// Joins the group with the specified ID.
    /// </summary>
    public void JoinGroup(long groupId) => Interceptor.Send(Out.JoinHabboGroup, groupId);

    /// <summary>
    /// Leaves the group with the specified ID.
    /// </summary>
    public void LeaveGroup(long groupId) => Interceptor.Send(Out.KickMember, groupId, UserId, false);

    /// <summary>
    /// Sets the specified group as the user's favourite group.
    /// </summary>
    /// <param name="groupId">The ID of the group to set as the user's favourite.</param>
    public void SetGroupFavourite(long groupId) => Interceptor.Send(Out.SelectFavouriteHabboGroup, groupId);

    /// <summary>
    /// Unsets the specified group as the user's favourite group.
    /// </summary>
    /// <param name="groupId">The ID of the group to remove from the user's favourite.</param>
    public void RemoveGroupFavourite(long groupId) => Interceptor.Send(Out.DeselectFavouriteHabboGroup, groupId);

    /// <summary>
    /// Gets the group information of the specified group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="timeout">The time to wait for a response from the server.</param>
    public IGroupData GetGroup(long groupId, int timeout = DEFAULT_TIMEOUT) =>
        new GetGroupDataTask(Interceptor, groupId).Execute(timeout, Ct);

    /// <summary>
    /// Gets a paged list of group members in the specified group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="page">The page number. (starting at 0)</param>
    /// <param name="filter">The filter text.</param>
    /// <param name="searchType">The type of member to search for.</param>
    /// <param name="timeout">The time to wait for a response from the server.</param>
    public IGroupMembers GetGroupMembers(long groupId, int page = 0, string filter = "",
        GroupMemberSearchType searchType = GroupMemberSearchType.Members, int timeout = DEFAULT_TIMEOUT)
        => new GetGroupMembersTask(Interceptor, groupId, page, filter, searchType).Execute(timeout, Ct);

    /// <summary>
    /// Accepts a user into the specified group.
    /// </summary>
    public void AcceptGroupMember(long groupId, long userId) => Interceptor.Send(Out.ApproveMembershipRequest, groupId, userId);

    /// <summary>
    /// Rejects a user from joining the specified group.
    /// </summary>
    public void RejectGroupMember(long groupId, long userId) => Interceptor.Send(Out.RejectMembershipRequest, groupId, userId);

    /// <summary>
    /// Kicks a user from the specified group.
    /// </summary>
    public void KickGroupMember(long groupId, long userId) => Interceptor.Send(Out.KickMember, groupId, userId, false);
}
