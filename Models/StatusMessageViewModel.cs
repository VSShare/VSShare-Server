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

        public MessageType Type { get; set; } = MessageType.Info;
    }

    public enum MessageType
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }
}
