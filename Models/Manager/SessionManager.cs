using ProtocolModels.Models;
using ProtocolModels.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Extensions;

namespace Server.Models.Manager
{
    public class SessionManager
    {
        public string Id { get; set; }

        public ContentType ContentType { get; set; }

        public string FileName { get; set; }

        private List<Line> Content { get; set; } = new List<Line>();

        public void UpdateContent(UpdateContentNotification notification)
        {
            lock (this.Content)
            {
                switch (notification.Type)
                {
                    case UpdateType.Delete:
                        if (this.Content.IsInRange(notification.Position, notification.Length))
                            this.Content.RemoveRange((int)notification.Position, (int)notification.Length);
                        break;
                    case UpdateType.Insert:
                        if (notification.Content == null)
                            return;

                        if (this.Content.IsInRange(notification.Position))
                            this.Content.InsertRange((int)notification.Position, notification.Content);
                        break;
                    case UpdateType.Replace:
                        if (notification.Content == null)
                            return;

                        if (this.Content.IsInRange(notification.Position, notification.Length))
                        {
                            // まず削除
                            this.Content.RemoveRange((int)notification.Position, (int)notification.Length);

                            // 次に挿入
                            this.Content.InsertRange((int)notification.Position, notification.Content);
                        }
                        break;
                    case UpdateType.RemoveMarker:
                        foreach (var item in this.Content)
                            item.IsModified = false;

                        break;
                    case UpdateType.ResetAll:
                        this.Content.Clear();
                        break;
                    case UpdateType.Append:
                    default:
                        if (notification.Content == null)
                            return;

                        this.Content.AddRange(notification.Content);
                        break;
                }
            }
        }
    }
}
