using ProtocolModels.Models;
using ProtocolModels.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Extensions;
using ProtocolModels.Broadcaster;

namespace Server.Models.Manager
{
    public class SessionManager
    {
        public string Id { get; set; }

        public string BroadcasterId { get; set; }

        public string BroadcasterName { get; set; }

        public ContentType ContentType { get; set; }

        public string FileName { get; set; }

        public List<Line> Content { get; private set; } = new List<Line>();

        public CursorType CursorType { get; private set; } = CursorType.Point;

        public CursorPosition CursorActivePosition { get; private set; }

        private CursorPosition _cursorAnchorPosition = new CursorPosition();

        public CursorPosition CursorAnchorPosition
        {
            get {
                return this.CursorType == CursorType.Point ? new CursorPosition() : this._cursorAnchorPosition;
            }
            private set { this._cursorAnchorPosition = value; }
        }

        public void UpdateContent(UpdateContentRequest request)
        {
            //var cloned =
            //    (from item in this.Content
            //     select new Line() {IsModified = item.IsModified, Text = item.Text}).ToList();
            //var isUpdated = true;

            var ordered = (from item in request.Data orderby item.Order ascending select item);
            lock (this.Content)
            {
                foreach (var data in ordered)
                {
                    switch (data.Type)
                    {
                        case UpdateType.Delete:
                            if (this.Content.IsInRange(data.Position, data.Length))
                                this.Content.RemoveRange((int)data.Position, (int)data.Length);
                            break;
                        case UpdateType.Insert:
                            if (data.Content == null)
                                continue;

                            if (this.Content.IsInRange(data.Position))
                                this.Content.InsertRange((int)data.Position, data.Content);
                            break;
                        case UpdateType.Replace:
                            if (data.Content == null)
                                continue;

                            if (this.Content.IsInRange(data.Position, data.Length))
                            {
                                // まず削除
                                this.Content.RemoveRange((int)data.Position, (int)data.Length);

                                // 次に挿入
                                this.Content.InsertRange((int)data.Position, data.Content);
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
                            if (data.Content == null)
                                continue;

                            this.Content.AddRange(data.Content);
                            break;
                    }
                }
            }
        }

        public bool UpdateCursor(UpdateCursorRequest request)
        {
            switch (request.Type)
            {
                case CursorType.Select:
                    if (request.Anchor == null || request.Active == null)
                        return false;
                    break;
                case CursorType.Point:
                default:
                    if (request.Active == null)
                        return false;

                    request.Anchor = null;
                    break;
            }

            this.CursorActivePosition = request.Active;
            this.CursorAnchorPosition = request.Anchor;
            this.CursorType = request.Type;

            return true;
        }

        #region "Listener Request"
        public UpdateCursorRequest GetCursor()
        {
            return new UpdateCursorRequest()
            {
                Active = this.CursorActivePosition,
                Anchor = this.CursorAnchorPosition,
                SessionId = this.Id,
                Type = this.CursorType
            };
        }

        public UpdateContentRequest GetContent()
        {
            return new UpdateContentRequest()
            {
                SessionId = this.Id,
                Data = new List<UpdateContentData>(new[]
                {
                  new UpdateContentData()
                  {
                      Content = this.Content,
                      Type = UpdateType.Append,
                      Order = 0
                  }
                })
            };
        }

        #endregion


    }

}
