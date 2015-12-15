using Microsoft.AspNet.SignalR;
using ProtocolModels.Broadcaster;
using ProtocolModels.Notification;
using ProtocolModels.Notifications;
using Server.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models.Manager
{
    public class RoomInfo
    {
        public string RoomId { get; set; }

        public Room Room { get; set; }

        public HashSet<string> Listeners { get; set; } = new HashSet<string>();

        public HashSet<string> Broadcasters { get; set; } = new HashSet<string>();

        public long ViewCount { get; set; } = 0;

        public long VisitorCount { get; set; } = 0;

        public bool IsBroadcasting { get; set; } = false;

        public Dictionary<string, SessionManager> Sessions { get; } = new Dictionary<string, SessionManager>();

        public SessionManager GetSession(string id)
        {
            if (this.Sessions.ContainsKey(id))
            {
                return this.Sessions[id];
            }

            return null;
        }

        public bool IsOwnerSession(string sessionId, string connectionId)
        {
            if (!this.Sessions.ContainsKey(sessionId))
            {
                return false;
            }

            var session = this.Sessions[sessionId];
            return (session.BroadcasterId == connectionId);
        }

        //public async Task NotifyStartBroadcast()
        //{
        //    var item = new BroadcastEventNotification()
        //    {
        //        EventType = BroadcastEventType.StartBroadcast
        //    };

        //    var manager = ListenerManager.GetInstance();

        //    await Task.Run(() =>
        //    {
        //        IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<ListenHub>();
        //        hubContext.Clients.Group(this.RoomId).NotifyBroadcastEvent(item);
        //    });
        //}

        public async Task NotifyStopBroadcast()
        {
            var item = new BroadcastEventNotification()
            {
                EventType = BroadcastEventType.StopBroadcast
            };
            var manager = ListenerManager.GetInstance();

            await Task.Run(() =>
            {
                IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<ListenHub>();
                hubContext.Clients.Group(this.RoomId).NotifyBroadcastEvent(item);
            });
        }

        public async Task UpdateRoomStatus()
        {
            var item = new UpdateBroadcastStatusNotification()
            {
                VisitorCount = this.VisitorCount,
                ViewCount = this.ViewCount
            };
            var manager = ListenerManager.GetInstance();

            await Task.Run(() =>
            {
                IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<ListenHub>();
                hubContext.Clients.Group(this.RoomId).UpdateRoomStatus(item);
            });
        }

        public async Task AppendSession(string id, AppendSessionRequest item, string connectionId, string nickName)
        {
            if (!this.Sessions.ContainsKey(id))
            {
                var session = new SessionManager()
                {
                    Id = id,
                    ContentType = item.ContentType,
                    FileName = item.FileName,
                    BroadcasterId = connectionId,
                    BroadcasterName = nickName
                };

                this.Sessions.Add(id, session);

                var manager = ListenerManager.GetInstance();
                var notification = new AppendSessionNotification()
                {
                    Id = id,
                    ContentType = item.ContentType,
                    FileName = item.FileName,
                    BroadcasterName = nickName
                };
                await Task.Run(() =>
                {
                    IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<ListenHub>();
                    hubContext.Clients.Group(this.RoomId).AppendSession(notification);
                });
            }
        }

        public async Task RemoveSession(RemoveSessionRequest item)
        {
            if (this.Sessions.ContainsKey(item.SessionId))
            {
                this.Sessions.Remove(item.SessionId);

                var manager = ListenerManager.GetInstance();
                await Task.Run(() =>
                {
                    Parallel.ForEach(this.Listeners, c =>
                    {
                        var listener = manager.GetConnectionInfo(c);
                        if (listener != null)
                        {
                            lock (listener)
                            {
                                IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<ListenHub>();
                                hubContext.Clients.Group(this.RoomId).RemoveSession(item);
                            }
                        }
                    });
                });
            }
        }

        #region "未定"
        //public async Task SwitchActiveSession(SwitchActiveSessionNotification item)
        //{
        //    if (this.Sessions.ContainsKey(item.SessionId))
        //    {
        //        this.CurrentSession = this.Sessions[item.SessionId];

        //        var manager = ListenerManager.GetInstance();
        //        await Task.Run(() =>
        //        {
        //            Parallel.ForEach(this.Listeners, c =>
        //            {
        //                var listener = manager.GetConnectionInfo(c);
        //                if (listener != null)
        //                {
        //                    lock (listener)
        //                    {
        //                        IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<ListenHub>();
        //                        hubContext.Clients.Group(this.RoomId).SwitchActiveSession(item);
        //                    }
        //                }
        //            });
        //        });
        //    }
        //}
        #endregion

        public async Task UpdateSessionInfo(UpdateSessionInfoRequest item)
        {
            if (this.Sessions.ContainsKey(item.SessionId))
            {
                var session = this.Sessions[item.SessionId];
                session.ContentType = item.ContentType;
                session.FileName = item.FileName;

                var notification = new AppendSessionNotification()
                {
                    Id = item.SessionId,
                    ContentType = item.ContentType,
                    FileName = item.FileName,
                    BroadcasterName = session.BroadcasterName
                };

                var manager = ListenerManager.GetInstance();
                await Task.Run(() =>
                {
                    IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<ListenHub>();
                    hubContext.Clients.Group(this.RoomId).UpdateSessionInfo(notification);
                });
            }
        }

        public async Task UpdateSessionContent(UpdateContentRequest item)
        {
            var session = this.GetSession(item.SessionId);
            if (session != null)
            {
                session.UpdateContent(item);
                await Task.Run(() =>
                {
                    IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<ListenHub>();
                    hubContext.Clients.Group(this.RoomId).UpdateSessionContent(item);
                });
            }
        }

        public async Task UpdateSessionCursor(UpdateCursorRequest item)
        {
            var session = this.GetSession(item.SessionId);
            if (session != null)
            {
                if (session.UpdateCursor(item))
                {
                    var manager = ListenerManager.GetInstance();
                    await Task.Run(() =>
                    {
                        IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<ListenHub>();
                        hubContext.Clients.Group(this.RoomId).UpdateSessionCursor(item);
                    });
                }
            }
        }

        #region "Listener Request"

        public List<AppendSessionNotification> GetSessionList()
        {
            return (from item in this.Sessions.Values
                    select new AppendSessionNotification()
                    {
                        BroadcasterName = item.BroadcasterName,
                        ContentType = item.ContentType,
                        FileName = item.FileName,
                        Id = item.Id
                    }).ToList();
        }

        #endregion


    }

}
