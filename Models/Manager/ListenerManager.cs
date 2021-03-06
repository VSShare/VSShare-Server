﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models.Manager
{
    public class ListenerManager
    {
        private static ListenerManager _instance = null;
        private Dictionary<string, ConnectionInfo> _connections = new Dictionary<string, ConnectionInfo>();

        private ListenerManager()
        {
        }

        public static ListenerManager GetInstance()
        {
            if (_instance == null)
                _instance = new ListenerManager();

            return _instance;
        }

        public ConnectionInfo GetConnectionInfo(string connectionId)
        {
            if (_connections.ContainsKey(connectionId))
            {
                return _connections[connectionId];
            }

            return null;
        }

        public bool IsListener(string connectionId)
        {
            return _connections.ContainsKey(connectionId);
        }

        public async Task<bool> RegisterListener(string connectionId, string roomId)
        {
            var roomManager = RoomManager.GetInstance();


            if (this._connections.ContainsKey(connectionId))
            {
                await roomManager.RemoveListener(connectionId, this._connections[connectionId].RoomId);

                this._connections.Remove(connectionId);
            }

            var result = await roomManager.RegisterListener(connectionId, roomId);

            if (result)
            {
                var info = new ConnectionInfo()
                {
                    ConnectionId = connectionId,
                    RoomId = roomId
                };

                this._connections.Add(connectionId, info);
            }

            return result;
        }

        public async Task<string>  RemoveListener(string connectionId)
        {
            if (this._connections.ContainsKey(connectionId))
            {
                var instance = RoomManager.GetInstance();
                var roomId = this._connections[connectionId].RoomId;
                await instance.RemoveListener(connectionId, this._connections[connectionId].RoomId);

                this._connections.Remove(connectionId);
                return roomId;
            }
            return null;
        }

        public void RemoveListenerWithoutRoomOperation(string connectionId)
        {
            this._connections.Remove(connectionId);
        }

    }
}
