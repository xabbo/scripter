using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;

using Xabbo.Core;
using Xabbo.Core.Tasks;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Gets the catalog of the specified type.
        /// </summary>
        /// <param name="type">The type of catalog to retrieve.</param>
        /// <param name="timeout">The time in milliseconds to wait for a response from the server.</param>
        public ICatalog GetCatalog(string type = "NORMAL", int timeout = DEFAULT_TIMEOUT)
            => new GetCatalogTask(Interceptor, type).Execute(timeout, Ct);

        /// <summary>
        /// Gets the builder's club catalog.
        /// </summary>
        /// <param name="timeout">The time in milliseconds to wait for a response from the server.</param>
        public ICatalog GetBcCatalog(int timeout = DEFAULT_TIMEOUT) => GetCatalog("BUILDERS_CLUB", timeout);

        /// <summary>
        /// Gets the specified catalog page from the catalog.
        /// </summary>
        /// <param name="pageId">The ID of the page to retrieve.</param>
        /// <param name="type">The catalog type to retrieve the page from.</param>
        /// <param name="timeout">The time in milliseconds to wait for a response from the server.</param>
        public ICatalogPage GetCatalogPage(int pageId, string type = "NORMAL", int timeout = DEFAULT_TIMEOUT)
            => new GetCatalogPageTask(Interceptor, pageId, type).Execute(timeout, Ct);

        /// <summary>
        /// Gets the specified catalog page from the builder's club catalog.
        /// </summary>
        /// <param name="pageId">The ID of the page to retrieve.</param>
        /// <param name="timeout">The time in milliseconds to wait for a response from the server.</param>
        public ICatalogPage GetBcCatalogPage(int pageId, int timeout = DEFAULT_TIMEOUT)
            => GetCatalogPage(pageId, "BUILDERS_CLUB", timeout);

        /// <summary>
        /// Gets the catalog page corresponding to the specified page node from the catalog.
        /// </summary>
        /// <param name="node">The node of which to load the corresponding catalog page.</param>
        /// <param name="timeout">The time in milliseconds to wait for a response from the server.</param>
        public ICatalogPage GetCatalogPage(ICatalogPageNode node, int timeout = DEFAULT_TIMEOUT)
            => GetCatalogPage(node.Id, node.Catalog?.Type ?? "NORMAL", timeout);

        /// <summary>
        /// Sends a request to purchase the specified catalog offer.
        /// </summary>
        /// <param name="offer">The offer to purchase.</param>
        /// <param name="count">The number of items to purchase.</param>
        /// <param name="extra">
        /// The extra parameter.
        /// In case of trophies, this is the message to be displayed on the trophy.
        /// For group furni, this is the ID of the group as a string.
        /// </param>
        public void Purchase(ICatalogOffer offer, int count = 1, string extra = "")
        {
            if (offer.Page is null)
                throw new Exception("Catalog page cannot be null.");

            Purchase(offer.Page.Id, offer.Id, count, extra);
        }

        /// <summary>
        /// Sends a request to purchase the specified catalog offer.
        /// </summary>
        /// <param name="pageId">The ID of the catalog page.</param>
        /// <param name="offerId">The ID of the offer to purchase.</param>
        /// <param name="count">The number of items to purchase.</param>
        /// <param name="extra">
        /// The extra parameter.
        /// In case of trophies, this is the message to be displayed on the trophy.
        /// For group furni, this is the ID of the group as a string.
        /// </param>
        public void Purchase(int pageId, int offerId, int count = 1, string extra = "")
        {
            Send(Out.PurchaseFromCatalog, pageId, offerId, extra, count);
        }

        /// <summary>
        /// Sends a request to purchase the specified catalog offer as a gift.
        /// </summary>
        /// <param name="offer">The catalog offer to purchase.</param>
        /// <param name="recipientName">The name of the recipient to send to.</param>
        /// <param name="giftMessage">The message to display on the gift.</param>
        /// <param name="extra">
        /// The extra parameter.
        /// In case of trophies, this is the message to be displayed on the trophy.
        /// For group furni, this is the ID of the group as a string.
        /// </param>
        /// <param name="giftFurniIdentifier">
        /// The gift furni identifier.
        /// If none is specified, a random one from
        /// <c>present_gen</c> to <c>present_gen6</c> will be chosen.
        /// </param>
        /// <param name="boxType"></param>
        /// <param name="decorationType"></param>
        public void PurchaseAsGift(ICatalogOffer offer, string recipientName,
            string giftMessage = "", string extra = "",
            string? giftFurniIdentifier = null, int boxType = 0, int decorationType = 0)
        {
            if (string.IsNullOrWhiteSpace(giftFurniIdentifier))
            {
                giftFurniIdentifier = $"present_gen";
            }
        }

        static class GiftFurni
        {
            public const string Basic = "present_gen";
            public const string Basic1 = "present_gen1";
            public const string Basic2 = "present_gen2";
            public const string Basic3 = "present_gen3";
            public const string Basic4 = "present_gen4";
            public const string Basic5 = "present_gen5";
            public const string Basic6 = "present_gen6";
        }

        public enum GiftBoxes
        {
            Royal = 0,
            Imperial = 1,
            Glamor = 2,
            Cardboard = 3,
            Steel = 4,
            IceCube = 5,
            Wooden = 6,
            Valentines = 8
        }

        public enum GiftDecorations
        {
            RedSilkKnotRibbon = 0,
            GoldenSilkKnotRibbon = 1,
            BlueSilkKnotRibbon = 2,
            PinkBow = 3,
            GreenBow = 4,
            WhiteBow = 5,
            PlainRibbon = 6,
            OrganicRibbon = 7,
            Suspenders = 8,
            Chains = 9,
            None = 10
        }
    }
}
