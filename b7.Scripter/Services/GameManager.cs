using System;
using System.Threading.Tasks;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.Interceptor.Dispatcher;
using Xabbo.Core.Game;

namespace b7.Scripter.Services
{
    public class GameManager : IGameManager
    {
        private readonly IMessageManager _messages;
        private readonly IRemoteInterceptor _interceptor;

        public Task SendAsync(Header header, params object[] values) => SendAsync(Packet.Compose(header, values));
        public Task SendAsync(IReadOnlyPacket packet) => _interceptor.SendAsync(packet);

        public event EventHandler? InitializeComponents;

        public ProfileManager ProfileManager { get; set; }
        public RoomManager RoomManager { get; set; }
        public TradeManager TradeManager { get; set; }
        public FriendManager FriendManager { get; set; }

        public GameManager(IMessageManager messages, IRemoteInterceptor interceptor)
        {
            _messages = messages;

            _interceptor = interceptor;
            _interceptor.Connected += Interceptor_ConnectionStart;
            _interceptor.Disconnected += Interceptor_ConnectionEnd;
            _interceptor.Disconnected += Interceptor_Disconnected;

            ProfileManager = new ProfileManager(interceptor);
            FriendManager = new FriendManager(interceptor);
            RoomManager = new RoomManager(interceptor);
            TradeManager = new TradeManager(interceptor, ProfileManager, RoomManager);
        }

        private void Interceptor_ConnectionStart(object? sender, EventArgs e)
        {
            // Dispatcher = new MessageDispatcher(_messages.Headers);
            // Components = new ComponentManager(this);
            // Components.LoadComponents(XabboComponent.GetCoreComponentTypes());

            // TODO Scoped component loading

            InitializeComponents?.Invoke(this, EventArgs.Empty);
        }

        private void Interceptor_ConnectionEnd(object? sender, EventArgs e)
        {

        }

        private void Interceptor_Disconnected(object? sender, EventArgs e)
        {

        }
    }
}
