using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using ProtocolModels.Auth;
using ProtocolModels.Broadcaster;
using Server.Hubs;

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

        public void RegisterBroadcaster(string connectionId, Room room)
        {
            if (this._rooms.ContainsKey(room.Id))
            {
                var temp = this._rooms[room.Id];
                temp.Broadcasters.Add(connectionId);
            }
            else
            {
                long visitorCount = room.TotalVisitor;

                var temp = new RoomInfo()
                {
                    RoomId = room.Id,
                    VisitorCount = visitorCount
                };
                temp.Broadcasters.Add(connectionId);

                this._rooms.Add(room.Id, temp);
            }
        }

        public async Task RemoveBroadcaster(string connectionId, string roomId)
        {
            if (this._rooms.ContainsKey(roomId))
            {
                var roomInfo = this._rooms[roomId];
                // Sessionが削除されたことを通知
                var sessions = roomInfo.Sessions.Where(c => c.Value.BroadcasterId == connectionId).Select(c => c.Key).ToList();

                foreach (var session in sessions)
                {
                    await roomInfo.RemoveSession(new RemoveSessionRequest()
                    {
                        SessionId = session
                    });
                }

                roomInfo.Broadcasters.Remove(connectionId);
                if (roomInfo.Broadcasters.Count == 0)
                {
                    using (var db = new ApplicationDbContext())
                    {
                        var room = await db.Rooms
                            .FirstOrDefaultAsync(c => c.Id == roomId);
                        if (room != null && room.IsLive)
                        {
                            // EFのLazyLoadingのため...
                            var owner = room.Owner;

                            // 自動的にDisconnectする
                            room.TotalVisitor = roomInfo.VisitorCount;
                            room.LatestBroadcastDate = DateTime.Now;
                            room.IsLive = false;
                            await db.SaveChangesAsync();
                        }
                    }


                    // Listenerへ通知
                    await roomInfo.NotifyStopBroadcast();

                    // Listenerの削除
                    var listenerManager = ListenerManager.GetInstance();
                    lock (roomInfo.Listeners)
                    {
                        IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<ListenHub>();

                        foreach (var listener in roomInfo.Listeners)
                        {
                            listenerManager.RemoveListenerWithoutRoomOperation(listener);
                            hubContext.Groups.Remove(listener, roomId).Wait();
                        }
                    }

                    // 放送の終了
                    //await room.NotifyStopBroadcast();
                    this._rooms.Remove(roomId);
                }
            }
        }

        public async Task<bool> RegisterListener(string connectionId, string roomId)
        {
            if (this._rooms.ContainsKey(roomId))
            {
                this._rooms[roomId].Listeners.Add(connectionId);
                this._rooms[roomId].VisitorCount++;

                await this._rooms[roomId].UpdateRoomStatus();
                return true;
            }
            // 含まれていない場合は何もしない（配信開始してない）
            return false;
        }

        public async Task RemoveListener(string connectionId, string roomId)
        {
            if (this._rooms.ContainsKey(roomId))
            {
                lock (this._rooms[roomId].Listeners)
                {
                    this._rooms[roomId].Listeners.Remove(connectionId);
                }

                await this._rooms[roomId].UpdateRoomStatus();
            }
        }
    }
}
