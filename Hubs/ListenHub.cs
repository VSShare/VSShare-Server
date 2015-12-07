using System;
using System.Collections.Generic;
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

            // TODO: 認証処理 from DB
            var roomId = "";

            // OKならDBに登録(IsOpening=True)

            await instance.RegisterListener(connectionId, roomId);
            var response = new AuthorizeListenerResponse()
            {
                IsSuccess = true
            };

            return response;
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