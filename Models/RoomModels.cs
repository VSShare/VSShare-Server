using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models
{

    public class Room
    {
        [Key()]
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public bool IsOpened { get; set; }

    }


}
