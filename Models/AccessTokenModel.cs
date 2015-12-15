using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models
{
    public class UserAccessToken
    {
        [Key()]
        public long Id { get; set; } 

        [Required()]
        public virtual ApplicationUser User { get; set; }

        [Required()]
        public string AccessToken { get; set; }

    }
}
