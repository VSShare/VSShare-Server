using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Server.Models.Manager;
using ProtocolModels.Auth;

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
            var instance = BroadcasterManager.GetInstance();

            // TODO: 認証処理 from DB
            var roomId = "";

            // OKならDBに登録(IsOpening=True)

            await instance.RegisterBroadcaster(connectionId, roomId);
            var response = new AuthorizeListenerResponse()
            {
                IsSuccess = true
            };

            return response;
        }

    }
}