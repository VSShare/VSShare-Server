using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Server.Models.Manager;
using ProtocolModels.Auth;
using ProtocolModels.Notifications;

namespace Server.Hubs
{
    [HubName("broadcast")]
    public class BroadcastHub : Hub
    {
        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (instance.IsBroadcaster(connectionId))
            {
                instance.RemoveBroadcaster(connectionId).Wait();
            }

            return base.OnDisconnected(stopCalled);
        }

        public async Task<AuthorizeBroadcasterResponse> Authorize(AuthorizeBroadcasterRequest request)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();

            // TODO: 認証処理 from DB
            var roomId = "";

            // OKならDBに登録(IsOpening=True)

            await instance.RegisterBroadcaster(connectionId, roomId);
            var response = new AuthorizeBroadcasterResponse()
            {
                IsSuccess = true
            };

            return response;
        }

        #region "Broadcast全般"

        public async Task StartBroadcast()
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return;

            var info = instance.GetConnectionInfo(connectionId);

            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            await room.NotifyStartBroadcast();
        }

        public async Task StopBroadcast()
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return;

            var info = instance.GetConnectionInfo(connectionId);

            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            await room.NotifyStopBroadcast();
        }

        #endregion

        #region "Session系"

        public async Task AppendSession(AppendSessionNotification item)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return;

            var info = instance.GetConnectionInfo(connectionId);
            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            await room.AppendSession(item);
        }

        public async Task RemoveSession(RemoveSessionNotification item)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return;

            var info = instance.GetConnectionInfo(connectionId);
            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            await room.RemoveSession(item);
        }

        public async Task SwitchActiveSession(SwitchActiveSessionNotification item)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return;

            var info = instance.GetConnectionInfo(connectionId);
            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            await room.SwitchActiveSession(item);
        }


        public async Task UpdateSession(UpdateSessionNotification item)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return;

            var info = instance.GetConnectionInfo(connectionId);
            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            await room.UpdateSession(item);
        }

        #endregion

        #region "Content系"

        public async Task UpdateContent(UpdateContentNotification item)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return;

            var info = instance.GetConnectionInfo(connectionId);
            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            await room.UpdateSessionContent(item);
        }

        #endregion
    }
}