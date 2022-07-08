using System;
using System.Threading.Tasks;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.Interceptor.Tasks;
using Xabbo.Core;

using Xabbo.Scripter.Runtime;

namespace Xabbo.Scripter.Tasks
{
    internal class EnterRoomTask : InterceptorTask<RoomEntryResult>
    {
        enum Status { RequestingRoomData, AwaitingFlatOpc, AwaitingRoomEntry }

        private readonly long _roomId;
        private readonly string? _password;

        private Status _state = Status.RequestingRoomData;

        public EnterRoomTask(IInterceptor interceptor, long roomId, string? password = null)
            : base(interceptor)
        {
            _roomId = roomId;
            _password = password;
        }

        protected override ValueTask OnExecuteAsync() => Interceptor.SendAsync(Out.GetGuestRoom, (LegacyLong)_roomId, 0, 1);

        [InterceptIn(nameof(Incoming.GetGuestRoomResult))]
        protected void HandleGetGuestRoomResult(InterceptArgs e)
        {
            if (_state != Status.RequestingRoomData) return;

            try
            {
                RoomData roomData = RoomData.Parse(e.Packet);
                if (roomData.Id == _roomId)
                {
                    roomData.IsEntering = false;
                    roomData.Forward = true;
                    roomData.Access = RoomAccess.Open;

                    e.Packet = new Packet(Interceptor.Client, e.Packet.Header)
                        .Write(roomData);

                    _state = Status.AwaitingFlatOpc;
                }
            }
            catch (Exception ex)
            {
                SetException(ex);
            }
        }

        [InterceptOut(nameof(Outgoing.FlatOpc))]
        protected void HandleFlatOpc(InterceptArgs e)
        {
            if (_state != Status.AwaitingFlatOpc) return;

            try
            {
                long roomId = e.Packet.ReadLegacyLong();
                if (roomId == _roomId)
                {
                    e.Packet.ReplaceString(_password ?? string.Empty);
                    _state = Status.AwaitingRoomEntry;
                }
            }
            catch (Exception ex)
            {
                SetException(ex);
            }
        }

        [InterceptIn(nameof(Incoming.RoomEntryInfo))]
        protected void HandleRoomEntryInfo(InterceptArgs e)
        {
            if (_state != Status.AwaitingRoomEntry) return;

            SetResult(RoomEntryResult.Success);
        }
    }
}
