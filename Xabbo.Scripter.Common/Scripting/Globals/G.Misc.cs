using System;

using Xabbo.Core;
using Xabbo.Core.Tasks;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
    /*
            TODO
                GetUserInfoWeb(int userId)
                GetUserProfileWeb(string uniqueId)

                GetOwnGroups()
                GetOwnRooms()

                GetPetInfo(Pet)
                GetPetInfo(petId)
        */

    /// <summary>
    /// Searches for the specified user by name and returns the matching search result if it exists.
    /// </summary>
    /// <param name="name">The name of the user to search for.</param>
    /// <param name="timeout">The time to wait for a response from the server.</param>
    public UserSearchResult? SearchUser(string name, int timeout = DEFAULT_TIMEOUT)
        => new SearchUserTask(Interceptor, name).Execute(timeout, Ct).GetResult(name);

    /// <summary>
    /// Searches for users by name and returns the results.
    /// </summary>
    /// <param name="name">The name of the user to search for.</param>
    /// <param name="timeout">The time to wait for a response from the server.</param>
    public UserSearchResults SearchUsers(string name, int timeout = DEFAULT_TIMEOUT)
        => new SearchUserTask(Interceptor, name).Execute(timeout, Ct);

    /// <summary>
    /// Gets the profile of the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="timeout">The time to wait for a response from the server.</param>
    public IUserProfile GetProfile(long userId, int timeout = DEFAULT_TIMEOUT)
        => new GetProfileTask(Interceptor, userId).Execute(timeout, Ct);
}
