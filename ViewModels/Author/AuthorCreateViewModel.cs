using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Author
{
    public class AuthorCreateViewModel
    {
        [Required(ErrorMessage = "Tên tác giả là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên tác giả không được vượt quá 100 ký tự")]
        [Display(Name = "Tên tác giả")]
        public string Name { get; set; } = null!;
        
        [StringLength(500, ErrorMessage = "Tiểu sử không được vượt quá 500 ký tự")]
        [Display(Name = "Tiểu sử")]
        [DataType(DataType.MultilineText)]
        public string? Bio { get; set; }
    }
}
