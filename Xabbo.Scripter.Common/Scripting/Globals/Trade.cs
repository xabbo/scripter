using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xabbo.Messages;
using Xabbo.Core;
using Xabbo.Core.Events;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Gets if the user is currently trading.
        /// </summary>
        public bool IsTrading => _tradeManager.IsTrading;
        /// <summary>
        /// Gets if the user is the one who initiated the trade.
        /// </summary>
        public bool IsTrader => _tradeManager.IsTrader;
        /// <summary>
        /// Gets if the user has accepted the trade.
        /// </summary>
        public bool HasAcceptedTrade => _tradeManager.HasAccepted;
        /// <summary>
        /// Gets if the trade partner has accepted the trade.
        /// </summary>
        public bool HasPartnerAcceptedTrade => _tradeManager.HasPartnerAccepted;
        /// <summary>
        /// Gets if both users have accepted the trade and are awaiting confirmation.
        /// </summary>
        public bool IsTradeWaitingConfirmation => _tradeManager.IsWaitingConfirmation;
        /// <summary>
        /// Gets the trade partner's <see cref="IRoomUser"/> instance.
        /// </summary>
        public IRoomUser? TradePartner => _tradeManager.Partner;
        /// <summary>
        /// Gets the user's own trade offer.
        /// </summary>
        public ITradeOffer? OwnTradeOffer => _tradeManager.OwnOffer;
        /// <summary>
        /// Gets the trade partner's trade offer.
        /// </summary>
        public ITradeOffer? PartnerTradeOffer => _tradeManager.PartnerOffer;

        // TODO EnsureTrade method that returns a result when the trade opens / fails to open

        /// <summary>
        /// Trades the specified user.
        /// </summary>
        public void Trade(IRoomUser user) => Trade(user.Index);

        /// <summary>
        /// Trades the user with the specified index.
        /// </summary>
        public void Trade(int userIndex) => Send(Out.TradeOpen, userIndex);

        /// <summary>
        /// Offers the specified inventory item in the trade.
        /// </summary>
        public void Offer(IInventoryItem item) => Offer(item.ItemId);

        /// <summary>
        /// Offers the item with the specified item ID in the trade.
        /// </summary>
        public void Offer(long itemId) => Send(Out.TradeAddItem, (LegacyLong)itemId);

        /// <summary>
        /// Offers the specified inventory items in the trade.
        /// </summary>
        public void Offer(IEnumerable<IInventoryItem> items) => Offer(items.Select(item => item.ItemId));

        /// <summary>
        /// Offers the items with the specified item IDs in the trade.
        /// </summary>
        public void Offer(IEnumerable<long> itemIds) => Send(Out.TradeAddItems, itemIds.Cast<LegacyLong>());

        /// <summary>
        /// Cancels the offer for the specified item in the trade.
        /// </summary>
        public void CancelOffer(IInventoryItem item) => CancelOffer(item.ItemId);

        /// <summary>
        /// Cancels the offer for the item with the specified item ID in the trade.
        /// </summary>
        public void CancelOffer(long itemId) => Send(Out.TradeRemoveItem, itemId);

        /// <summary>
        /// Cancels the trade.
        /// </summary>
        public void CancelTrade() => Send(Out.TradeClose);

        /// <summary>
        /// Accepts the trade.
        /// </summary>
        public void AcceptTrade() => Send(Out.TradeAccept);

        /// <summary>
        /// Confirms the trade.
        /// </summary>
        public void ConfirmTrade() => Send(Out.TradeConfirmAccept);

        /// <summary>
        /// Registers a callback that is invoked when a trade is started.
        /// </summary>
        public void OnTradeOpened(Action<TradeStartEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.Opened), callback);
        /// <summary>
        /// Registers a callback that is invoked when a trade is started.
        /// </summary>
        public void OnTradeOpened(Func<TradeStartEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.Opened), callback);

        /// <summary>
        /// Registers a callback that is invoked when a trade fails to start.
        /// </summary>
        public void OnTradeOpenFailed(Action<TradeStartFailEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.OpenFailed), callback);
        /// <summary>
        /// Registers a callback that is invoked when a trade fails to start.
        /// </summary>
        public void OnTradeOpenFailed(Func<TradeStartFailEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.OpenFailed), callback);

        /// <summary>
        /// Registers a callback that is invoked when a trade is updated.
        /// </summary>
        public void OnTradeUpdated(Action<TradeOfferEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.Updated), callback);
        /// <summary>
        /// Registers a callback that is invoked when a trade is updated.
        /// </summary>
        public void OnTradeUpdated(Func<TradeOfferEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.Updated), callback);

        /// <summary>
        /// Registers a callback that is invoked when a user accepts the trade.
        /// </summary>
        public void OnTradeAccepted(Action<TradeAcceptEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.Accepted), callback);
        /// <summary>
        /// Registers a callback that is invoked when a user accepts the trade.
        /// </summary>
        public void OnTradeAccepted(Func<TradeAcceptEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.Accepted), callback);

        /// <summary>
        /// Registers a callback that is invoked when both users have accepted the trade and are waiting for each other's confirmation.
        /// </summary>
        public void OnTradeWaitingConfirm(Action<EventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.WaitingConfirm), callback);
        /// <summary>
        /// Registers a callback that is invoked when both users have accepted the trade and are waiting for each other's confirmation.
        /// </summary>
        public void OnTradeWaitingConfirm(Func<EventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.WaitingConfirm), callback);

        /// <summary>
        /// Registers a callback that is invoked when a trade is closed.
        /// </summary>
        public void OnTradeClosed(Action<TradeStopEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.Closed), callback);
        /// <summary>
        /// Registers a callback that is invoked when a trade is closed.
        /// </summary>
        public void OnTradeClosed(Func<TradeStopEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.Closed), callback);

        /// <summary>
        /// Registers a callback that is invoked when a trade completes successfully.
        /// </summary>
        public void OnTradeCompleted(Action<TradeCompleteEventArgs> callback) => Register(_tradeManager, nameof(_tradeManager.Completed), callback);
        /// <summary>
        /// Registers a callback that is invoked when a trade completes successfully.
        /// </summary>
        public void OnTradeCompleted(Func<TradeCompleteEventArgs, Task> callback) => Register(_tradeManager, nameof(_tradeManager.Completed), callback);
    }
}
