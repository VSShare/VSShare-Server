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
using ProtocolModels.Broadcaster;
using ProtocolModels.Listener;
using ProtocolModels.Notification;
using Server.Models;

namespace Server.Hubs
{
    [HubName("listen")]
    public class ListenHub : Hub
    {
        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var connectionId = Context.ConnectionId;
            var instance = ListenerManager.GetInstance();
            if (instance.IsListener(connectionId))
            {
                instance.RemoveListener(connectionId).Wait();
            }

            return base.OnDisconnected(stopCalled);
        }

        public async Task<AuthorizeListenerResponse> Authorize(AuthorizeListenerRequest request)
        {
            var connectionId = Context.ConnectionId;
            var instance = ListenerManager.GetInstance();

            var tokenManager = TokenManager.GetInstance();
            var roomId = tokenManager.GetTokenInfo(request.Token);

            if (roomId == null)
                return new AuthorizeListenerResponse()
                {
                    IsSuccess = false
                };

            tokenManager.RemoveToken(request.Token);

            //// Broadcasterも立ち入り禁止になる...
            //using (var db = new ApplicationDbContext())
            //{
            //    var room = await db.Rooms.Where(c => c.Name == roomId).SingleOrDefaultAsync();
            //    if (room == null || !room.IsLive)
            //        return new AuthorizeListenerResponse()
            //        {
            //            IsSuccess = false
            //        };
            //}

            // OKならDBに登録(IsOpening=True)

            await instance.RegisterListener(connectionId, roomId);
            return new AuthorizeListenerResponse()
            {
                IsSuccess = true
            };
        }

        #region "Session Request系"

        public List<AppendSessionNotification> GetSessionList()
        {
            var connectionId = Context.ConnectionId;
            var instance = ListenerManager.GetInstance();
            if (instance.IsListener(connectionId))
            {
                var info = instance.GetConnectionInfo(connectionId);
                var roomInstance = RoomManager.GetInstance();
                var room = roomInstance.GetRoomInfo(info.RoomId);
                return room.GetSessionList();
            }
            return null;
        }

        public AppendSessionNotification GetSessionInfo(GetSessionRequest request)
        {
            var connectionId = Context.ConnectionId;
            var instance = ListenerManager.GetInstance();
            if (instance.IsListener(connectionId))
            {
                var info = instance.GetConnectionInfo(connectionId);
                var roomInstance = RoomManager.GetInstance();
                var room = roomInstance.GetRoomInfo(info.RoomId);
                var session = room.GetSession(request.Id);
                if (session != null)
                {
                    return session.GetSessionInfo();
                }
            }
            return null;
        }


        #endregion

        #region "Content Request系"

        public UpdateContentRequest GetSessionContent(GetContentRequest request)
        {
            var connectionId = Context.ConnectionId;
            var instance = ListenerManager.GetInstance();
            if (instance.IsListener(connectionId))
            {
                var info = instance.GetConnectionInfo(connectionId);
                var roomInstance = RoomManager.GetInstance();
                var room = roomInstance.GetRoomInfo(info.RoomId);
                var session = room.GetSession(request.Id);
                if (session != null)
                {
                    return session.GetContent();
                }
            }
            return null;
        }

        #endregion

        #region "Cursor Request系"

        public UpdateCursorRequest GetSessionCursor(GetCursorRequest request)
        {
            var connectionId = Context.ConnectionId;
            var instance = ListenerManager.GetInstance();
            if (instance.IsListener(connectionId))
            {
                var info = instance.GetConnectionInfo(connectionId);
                var roomInstance = RoomManager.GetInstance();
                var room = roomInstance.GetRoomInfo(info.RoomId);
                var session = room.GetSession(request.Id);
                if (session != null)
                {
                    return session.GetCursor();
                }
            }
            return null;
        }
        #endregion
    }
}