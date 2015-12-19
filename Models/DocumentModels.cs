using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models
{
    public class Document
    {
        [Key()]
        public int Id { get; set; }

        [MaxLength(30)]
        [StringLength(30, MinimumLength = 3)]
        [Index(IsUnique = true)]
        [Display(Name = "名前")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "英数字のみ使えます。")]
        public string Name { get; set; }

        [MaxLength(300)]
        [Display(Name = "タイトル")]
        public string Title { get; set; }

        [MaxLength(10000)]
        [Display(Name = "本文")]
        [DataType(DataType.MultilineText)]
        public string Body { get; set; }

        [Display(Name = "作成日")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "最終更新日")]
        public DateTime ModifiedAt { get; set; }

        [Required()]
        [Display(Name = "作成者")]
        public ApplicationUser Owner { get; set; }

        [Display(Name = "表示順")]
        public int Index { get; set; }

    }

    public class CreateDocumentViewModel
    {
        [MaxLength(30)]
        [StringLength(30, MinimumLength = 3)]
        [Index(IsUnique = true)]
        [Display(Name = "名前")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "英数字のみ使えます。")]
        public string Name { get; set; }

        [MaxLength(300)]
        [Display(Name = "タイトル")]
        public string Title { get; set; }

        [MaxLength(10000)]
        [Display(Name = "本文")]
        [DataType(DataType.MultilineText)]
        public string Body { get; set; }

        [Display(Name = "表示順")]
        public int Index { get; set; }

    }


    public class EditDocumentViewModel
    {
        [MaxLength(30)]
        [StringLength(30, MinimumLength = 3)]
        [Display(Name = "名前")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "英数字のみ使えます。")]
        public string Name { get; set; }

        [MaxLength(300)]
        [Display(Name = "タイトル")]
        public string Title { get; set; }

        [MaxLength(10000)]
        [Display(Name = "本文")]
        [DataType(DataType.MultilineText)]
        public string Body { get; set; }

        [Display(Name = "表示順")]
        public int Index { get; set; }

    }

}
