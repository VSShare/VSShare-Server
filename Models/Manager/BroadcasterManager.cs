using Microsoft.AspNet.SignalR;
using ProtocolModels.Notifications;
using Server.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models.Manager
{
    public class BroadcasterManager
    {
        private static BroadcasterManager _instance = null;

        private BroadcasterManager()
        {
        }

        public static BroadcasterManager GetInstance()
        {
            if (_instance == null)
                _instance = new BroadcasterManager();

            return _instance;
        }

        private Dictionary<string, ConnectionInfo> _connections = new Dictionary<string, ConnectionInfo>();

        public ConnectionInfo GetConnectionInfo(string connectionId)
        {
            if (_connections.ContainsKey(connectionId))
            {
                return _connections[connectionId];
            }
            return null;
        }

        public bool IsBroadcaster(string connectionId)
        {
            return _connections.ContainsKey(connectionId);
        }

        public async Task RegisterBroadcaster(string connectionId, string roomId)
        {
            var roomManager = RoomManager.GetInstance();

            if (this._connections.ContainsKey(connectionId))
            {
                await roomManager.RemoveBroadcaster(connectionId, this._connections[connectionId].RoomId);

                this._connections.Remove(connectionId);
            }

            roomManager.RegisterBroadcaster(connectionId, roomId);

            var info = new ConnectionInfo()
            {
                ConnectionId = connectionId,
                RoomId = roomId
            };

            this._connections.Add(connectionId, info);
        }

        public async Task RemoveBroadcaster(string connectionId)
        {
            
            if (this._connections.ContainsKey(connectionId))
            {
                var instance = RoomManager.GetInstance();
                await instance.RemoveBroadcaster(connectionId, this._connections[connectionId].RoomId);

                this._connections.Remove(connectionId);
            }
        }

    }
}
