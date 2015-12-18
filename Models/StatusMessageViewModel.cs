using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models
{
    public class StatusMessageViewModel<T>
    {
        public T Item { get; set; }

        public string Message { get; set; }

        public MessageType Type { get; set; } = MessageType.None;

        public string GetAlertClass()
        {
            switch (this.Type)
            {
                case MessageType.Success:
                    return "alert alert-success";
                case MessageType.Warning:
                    return "alert alert-warning";
                case MessageType.Error:
                    return "alert alert-error";
                case MessageType.Info:
                default:
                    return "alert alert-info";
            }
        }

    }

    public enum MessageType
    {
        None = -1,
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }

}
