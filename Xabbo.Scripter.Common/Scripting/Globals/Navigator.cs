using System;
using System.Collections.Generic;

using Xabbo.Core;
using Xabbo.Core.Tasks;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        #region - Navigator -
        /// <summary>
        /// Searches the navigator by category/filter and returns the list of navigator search results.
        /// </summary>
        /// <param name="category">The category to search.</param>
        /// <param name="filter">The filter text. Can be left empty.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public NavigatorSearchResults GetNav(string category, string filter = "", int timeout = DEFAULT_TIMEOUT)
            => new SearchNavigatorTask(Interceptor, category, filter).Execute(timeout, Ct);

        /// <summary>
        /// Searches the navigator by category/filter and returns a flattened view of <see cref="RoomInfo"/>.
        /// </summary>
        /// <param name="category">The category to search.</param>
        /// <param name="filter">The filter text. Can be left empty.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IEnumerable<IRoomInfo> SearchNav(string category, string filter = "", int timeout = DEFAULT_TIMEOUT)
            => GetNav(category, filter, timeout).GetRooms();

        /// <summary>
        /// Queries the navigator and returns a flattened view of <see cref="RoomInfo"/>.
        /// This is the same as searching by 'Anything' in the game client.
        /// </summary>
        /// <param name="query">The query text.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IEnumerable<IRoomInfo> QueryNav(string query, int timeout = DEFAULT_TIMEOUT)
            => GetNav("query", query, timeout).GetRooms();

        /// <summary>
        /// Searches the navigator by room name and returns a flattened view of <see cref="RoomInfo"/>.
        /// </summary>
        /// <param name="roomName">The room name to search for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        /// <returns></returns>
        public IEnumerable<IRoomInfo> SearchNavByName(string roomName, int timeout = DEFAULT_TIMEOUT)
            => GetNav("query", $"roomname:{roomName}", timeout).GetRooms();

        /// <summary>
        /// Searches the navigator by owner name and returns a flattened view of <see cref="RoomInfo"/>.
        /// </summary>
        /// <param name="ownerName">The room owner name to search for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IEnumerable<IRoomInfo> SearchNavByOwner(string ownerName, int timeout = DEFAULT_TIMEOUT)
            => GetNav("query", $"owner:{ownerName}", timeout).GetRooms();

        /// <summary>
        /// Searches the navigator by tag and returns a flattened view of <see cref="RoomInfo"/>.
        /// </summary>
        /// <param name="tag">The tag to search for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IEnumerable<IRoomInfo> SearchNavByTag(string tag, int timeout = DEFAULT_TIMEOUT)
            => GetNav("query", $"tag:{tag}", timeout).GetRooms();

        /// <summary>
        /// Searches the navigator by group name and returns a flattened view of <see cref="RoomInfo"/>.
        /// </summary>
        /// <param name="group">The group name to search for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IEnumerable<IRoomInfo> SearchNavByGroup(string group, int timeout = DEFAULT_TIMEOUT)
            => GetNav("query", $"group:{group}", timeout).GetRooms();
        #endregion
    }
}
