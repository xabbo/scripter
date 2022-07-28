using System;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.Core;
using Xabbo.Core.Tasks;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {

        /// <summary>
        /// Places a sticky at the specified location.
        /// </summary>
        public void PlaceSticky(IInventoryItem item, WallLocation location)
        {
            if (item.Category != FurniCategory.Sticky)
                throw new InvalidOperationException("The specified item is not a sticky note.");
            Interceptor.Send(Out.PlacePostIt, item.ItemId, location);
        }

        /// <summary>
        /// Places a sticky at the specified location.
        /// </summary>
        public void PlaceSticky(long itemId, WallLocation location) => Interceptor.Send(Out.PlacePostIt, itemId, location);

        /// <summary>
        /// Places a sticky at the specified location using a sticky pole.
        /// </summary>
        public void PlaceStickyWithPole(IInventoryItem item, WallLocation location, string color, string text)
            => PlaceStickyWithPole(item.ItemId, location, color, text);

        /// <summary>
        /// Places a sticky at the specified location using a sticky pole.
        /// </summary>
        public void PlaceStickyWithPole(long itemId, WallLocation location, string color, string text)
            => Interceptor.Send(Out.AddSpamWallPostIt, itemId, location, color, text);

        /// <summary>
        /// Gets the sticky data for the specified wall item.
        /// </summary>
        /// <param name="item">The sticky item to get data for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public Sticky GetSticky(IWallItem item, int timeout = DEFAULT_TIMEOUT) => GetSticky(item.Id, timeout);

        /// <summary>
        /// Gets the sticky data for the specified wall item.
        /// </summary>
        /// <param name="itemId">The item ID of the sticky to get data for.</param>
        /// <param name="timeout">The time to wait for a response from the server.</param>
        public Sticky GetSticky(long itemId, int timeout = DEFAULT_TIMEOUT)
            => new GetStickyTask(Interceptor, itemId).Execute(timeout, Ct);

        /// <summary>
        /// Updates the specified sticky.
        /// </summary>
        public void UpdateSticky(Sticky sticky) => UpdateSticky(sticky.Id, sticky.Color, sticky.Text);

        /// <summary>
        /// Updates the specified sticky.
        /// </summary>
        public void UpdateSticky(IWallItem item, string color, string text) => UpdateSticky(item.Id, color, text);

        /// <summary>
        /// Updates the specified sticky.
        /// </summary>
        public void UpdateSticky(long itemId, string color, string text) => Interceptor.Send(Out.SetStickyData, itemId, color, text);

        /// <summary>
        /// Deletes the specified sticky.
        /// </summary>
        public void DeleteSticky(Sticky sticky) => DeleteWallItem(sticky.Id);

    }
}
