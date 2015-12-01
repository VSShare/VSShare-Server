using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models.Manager
{
    public class RoomManager
    {
        private static RoomManager _instance = null;
        private Dictionary<string, RoomInfo> _rooms = new Dictionary<string, RoomInfo>();

        private RoomManager()
        {
        }

        public static RoomManager GetInstance()
        {
            if (_instance == null)
                _instance = new RoomManager();

            return _instance;
        }

        public RoomInfo GetRoomInfo(string roomId)
        {
            if (_rooms.ContainsKey(roomId))
            {
                return _rooms[roomId];
            }

            return null;
        }


        public bool IsOpened(string connectionId)
        {
            return _rooms.ContainsKey(connectionId);
        }

        public void RegisterBroadcaster(string connectionId, string roomId)
        {
            if (this._rooms.ContainsKey(roomId))
            {
                var room = this._rooms[roomId];
                room.Broadcasters.Add(connectionId);
            }
            else
            {
                long visitorCount = 0;
                // TODO: DBから取得

                var room = new RoomInfo()
                {
                    RoomId = roomId,
                    VisitorCount = visitorCount
                };
                room.Broadcasters.Add(connectionId);

                this._rooms.Add(roomId, room);
            }
        }

        public async Task RemoveBroadcaster(string connectionId, string roomId)
        {
            if (this._rooms.ContainsKey(roomId))
            {
                var room = this._rooms[roomId];
                room.Broadcasters.Remove(connectionId);
                if (room.Broadcasters.Count == 0)
                {
                    // TODO: DBへの反映(IsOpenedをFalseに)

                    // 放送の終了
                    await room.NotifyStopBroadcast();
                }
                this._rooms.Remove(roomId);
            }
        }

        public async Task RegisterListener(string connectionId, string roomId)
        {
            if (this._rooms.ContainsKey(roomId))
            {
                this._rooms[roomId].Listeners.Add(connectionId);

                await this._rooms[roomId].UpdateBroadcastStatus();
            }
            // 含まれていない場合は何もしない（配信開始してない）
        }

        public async Task RemoveListener(string connectionId, string roomId)
        {
            if (this._rooms.ContainsKey(roomId))
            {
                this._rooms[roomId].Listeners.Remove(connectionId);

                await this._rooms[roomId].UpdateBroadcastStatus();
            }
        }
    }
}
