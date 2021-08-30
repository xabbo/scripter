using System;

using Xabbo.Core.Game;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Gets the inventory of the user.
        /// Returns <c>null</c> if the inventory has not yet been loaded.
        /// Note that the inventory may be invalidated, in which case
        /// the items will be out of sync with the server.
        /// Call <see cref="EnsureInventory(int)"/> before accessing this
        /// property to ensure that the user's inventory has been loaded.
        /// </summary>
        IInventory? Inventory => _inventoryManager.Inventory;

        /// <summary>
        /// Ensures that the inventory of the user is loaded.
        /// Returns the inventory of the user immediately if it is already loaded and is not invalidated,
        /// otherwise attempts to retrieve it from the server.
        /// The user must be in a room for the server to return a response.
        /// </summary>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public IInventory EnsureInventory(int timeout = DEFAULT_LONG_TIMEOUT) =>
            _inventoryManager.GetInventoryAsync(timeout, Ct).GetAwaiter().GetResult();
    }
}
