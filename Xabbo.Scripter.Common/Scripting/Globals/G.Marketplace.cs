using System;
using System.Collections.Generic;

using Xabbo.Core;
using Xabbo.Core.GameData;
using Xabbo.Core.Tasks;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Gets the user's own marketplace offers.
        /// </summary>
        /// <param name="timeout">The time in milliseconds to wait for a response from the server.</param>
        public IUserMarketplaceOffers GetUserMarketplaceOffers(int timeout = DEFAULT_TIMEOUT)
            => new GetUserMarketplaceOffersTask(Interceptor).Execute(timeout, Ct);

        /// <summary>
        /// Searches for open offers in the marketplace.
        /// </summary>
        /// <param name="searchText">The name of the item to search for.</param>
        /// <param name="from">The minimum offer price in credits.</param>
        /// <param name="to">The maximum offer price in credits.</param>
        /// <param name="sort">The order in which to sort the results.</param>
        /// <param name="timeout">The time in milliseconds to wait for a response from the server.</param>
        /// <returns>The list of matching marketplace offers.</returns>
        public IEnumerable<IMarketplaceOffer> SearchMarketplace(
            string? searchText = null, int? from = null, int? to = null,
            MarketplaceSortOrder sort = MarketplaceSortOrder.HighestPrice,
            int timeout = DEFAULT_TIMEOUT)
            => new SearchMarketplaceTask(Interceptor, searchText, from, to, sort).Execute(timeout, Ct);

        /// <summary>
        /// Gets the marketplace information for the specified item type and kind.
        /// </summary>
        public IMarketplaceItemInfo GetMarketplaceInfo(ItemType type, int kind, int timeout = DEFAULT_TIMEOUT)
            => new GetMarketplaceInfoTask(Interceptor, type, kind).Execute(timeout, Ct);

        /// <summary>
        /// Gets the marketplace information for the specified item.
        /// </summary>
        public IMarketplaceItemInfo GetMarketplaceInfo(IItem item, int timeout = DEFAULT_TIMEOUT)
        {
            ArgumentNullException.ThrowIfNull(item);
            return new GetMarketplaceInfoTask(Interceptor, item.Type, item.Kind).Execute(timeout, Ct);
        }

        /// <summary>
        /// Gets the marketplace information for the specified furni.
        /// </summary>
        public IMarketplaceItemInfo GetMarketplaceInfo(FurniInfo furniInfo, int timeout = DEFAULT_TIMEOUT)
            => new GetMarketplaceInfoTask(Interceptor, furniInfo.Type, furniInfo.Kind).Execute(timeout, Ct);
    }
}
