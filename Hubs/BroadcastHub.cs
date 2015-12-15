using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Server.Models.Manager;
using ProtocolModels.Auth;
using ProtocolModels.Notifications;
using ProtocolModels.Broadcaster;
using Server.Models;
using Newtonsoft.Json;

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

            using (var db = new ApplicationDbContext())
            {
                var accessUser = await db.AccessTokens
                    .FirstOrDefaultAsync(c => c.AccessToken == request.AccessToken && c.User.UserName == request.UserName);

                var room = await db.Rooms
                    .FirstOrDefaultAsync(c => c.Name == request.RoomName && c.BroadcastToken == request.RoomToken);

                if (room == null || accessUser == null)
                    return new AuthorizeBroadcasterResponse()
                    {
                        IsSuccess = false
                    };

                await instance.RegisterBroadcaster(connectionId, room, accessUser.User);

                return new AuthorizeBroadcasterResponse()
                {
                    IsSuccess = true
                };
            }
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


        public async Task UpdateSessionInfo(UpdateSessionInfoRequest item)
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

            await room.UpdateSessionInfo(item);
        }

        #endregion

        #region "Content系"

        public async Task UpdateSessionContent(UpdateContentRequest item)
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

        public async Task UpdateSessionCursor(UpdateCursorRequest item)
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