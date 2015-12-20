using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Server.Models
{

    public class Room
    {
        [Key()]
        public string Id { get; set; }

        [Required]
        [MaxLength(30)]
        [StringLength(30, MinimumLength = 3)]
        [Index(IsUnique = true)]
        [Display(Name = "名前")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "英数字のみ使えます。")]
        public string Name { get; set; }

        [StringLength(30)]
        [Display(Name = "表示名")]
        public string DisplayName { get; set; }

        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        [Display(Name = "説明")]
        public string Description { get; set; }

        [Display(Name = "部屋一覧に非表示")]
        public bool IsHidden { get; set; }

        [Display(Name = "閲覧に認証が必要")]
        public bool IsPrivate { get; set; }

        [StringLength(50)]
        [Display(Name = "閲覧パスコード")]
        public string AccessCode { get; set; }

        [Required()]
        [StringLength(50)]
        [Display(Name = "ルームトークン")]
        public string BroadcastToken { get; set; }

        [Required]
        public virtual ApplicationUser Owner { get; set; }

        [Display(Name = "総閲覧者数")]
        public long TotalVisitor { get; set; }

        [Display(Name = "最終配信日")]
        public DateTime LatestBroadcastDate { get; set; }

        public bool IsLive { get; set; }

        [Display(Name = "作成日")]
        public DateTime CreatedAt { get; set; }

        public Room()
        {
        }
    }

    public enum RoomAction
    {
        StartBroadcast = 0,
        StopBroadcast = 1
    }

    public class CreateRoomViewModel
    {
        [Required]
        [Index(IsUnique = true)]
        [MaxLength(30)]
        [StringLength(30, MinimumLength = 3)]
        [Display(Name = "名前")]
        [Remote("IsRoomNameAvailable", "Rooms", ErrorMessage = "既に使われています。")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "英数字のみ使えます。")]
        public string Name { get; set; }

        [StringLength(30)]
        [Display(Name = "表示名")]
        public string DisplayName { get; set; }

        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        [Display(Name = "説明")]
        public string Description { get; set; }

        [Display(Name = "部屋一覧に非表示")]
        public bool IsHidden { get; set; }

        [Display(Name = "閲覧に認証が必要")]
        public bool IsPrivate { get; set; }

        [StringLength(50)]
        [Display(Name = "閲覧パスコード")]
        public string AccessCode { get; set; }

    }

    public class JoinRoomViewModel
    {
        
        public Room Room { get; set; }

        public bool CanJoin { get; set; } = false;

        public bool IsBroadcaster { get; set; } = false;


    }


}
