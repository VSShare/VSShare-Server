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
using ProtocolModels.Broadcaster;

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
                instance.RemoveBroadcaster(connectionId);
            }

            return base.OnDisconnected(stopCalled);
        }

        public AuthorizeBroadcasterResponse Authorize(AuthorizeBroadcasterRequest request)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();

            // TODO: 認証処理 from DB
            var roomId = "";
            var nickname = "";
            // OKならDBに登録(IsOpening=True)


            instance.RegisterBroadcaster(connectionId, roomId, nickname);
            var response = new AuthorizeBroadcasterResponse()
            {
                IsSuccess = true
            };

            return response;
        }

        #region "Broadcast全般"

        //public async Task StartBroadcast()
        //{
        //    var connectionId = Context.ConnectionId;
        //    var instance = BroadcasterManager.GetInstance();
        //    if (!instance.IsBroadcaster(connectionId))
        //        return;

        //    var info = instance.GetConnectionInfo(connectionId);

        //    var roomManager = RoomManager.GetInstance();
        //    var room = roomManager.GetRoomInfo(info.RoomId);
        //    await room.NotifyStartBroadcast();
        //}

        //public async Task StopBroadcast()
        //{
        //    var connectionId = Context.ConnectionId;
        //    var instance = BroadcasterManager.GetInstance();
        //    if (!instance.IsBroadcaster(connectionId))
        //        return;

        //    var info = instance.GetConnectionInfo(connectionId);

        //    var roomManager = RoomManager.GetInstance();
        //    var room = roomManager.GetRoomInfo(info.RoomId);
        //    await room.NotifyStopBroadcast();
        //}

        #endregion

        #region "Session系"

        public async Task<AppendSessionResponse> AppendSession(AppendSessionRequest item)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return new AppendSessionResponse() { IsSuccess = false };

            var info = instance.GetConnectionInfo(connectionId);
            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            var id = Guid.NewGuid().ToString();

            await room.AppendSession(id, item, connectionId, info.Nickname);
            return new AppendSessionResponse() { IsSuccess = true, Id = id };
        }

        public async Task RemoveSession(RemoveSessionRequest item)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return;

            var info = instance.GetConnectionInfo(connectionId);
            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            if (!room.IsOwnerSession(item.SessionId, connectionId))
                return;

            await room.RemoveSession(item);
        }

        //public async Task SwitchActiveSession(SwitchActiveSessionNotification item)
        //{
        //    var connectionId = Context.ConnectionId;
        //    var instance = BroadcasterManager.GetInstance();
        //    if (!instance.IsBroadcaster(connectionId))
        //        return;

        //    var info = instance.GetConnectionInfo(connectionId);
        //    var roomManager = RoomManager.GetInstance();
        //    var room = roomManager.GetRoomInfo(info.RoomId);
        //    await room.SwitchActiveSession(item);
        //}


        public async Task UpdateSession(UpdateSessionRequest item)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return;

            var info = instance.GetConnectionInfo(connectionId);
            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            if (!room.IsOwnerSession(item.SessionId, connectionId))
                return;

            await room.UpdateSession(item);
        }

        #endregion

        #region "Content系"

        public async Task UpdateContent(UpdateContentRequest item)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return;

            var info = instance.GetConnectionInfo(connectionId);
            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            if (!room.IsOwnerSession(item.SessionId, connectionId))
                return;

            await room.UpdateSessionContent(item);
        }

        #endregion

        #region "Cursor系"

        public async Task UpdateCursor(UpdateCursorRequest item)
        {
            var connectionId = Context.ConnectionId;
            var instance = BroadcasterManager.GetInstance();
            if (!instance.IsBroadcaster(connectionId))
                return;

            var info = instance.GetConnectionInfo(connectionId);
            var roomManager = RoomManager.GetInstance();
            var room = roomManager.GetRoomInfo(info.RoomId);
            if (!room.IsOwnerSession(item.SessionId, connectionId))
                return;

            await room.UpdateSessionCursor(item);
        }

        #endregion

    }
}