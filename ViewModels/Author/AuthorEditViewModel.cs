using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Author
{
    public class AuthorEditViewModel
    {
        public int AuthorId { get; set; }

        [Required(ErrorMessage = "Tên tác giả không được để trống")]
        [Display(Name = "Tên thật")]
        public string Name { get; set; } = null!;

        [Display(Name = "Bút danh")]
        public string? Pseudonym { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Quốc tịch")]
        public string? Nationality { get; set; }

        [Display(Name = "Tiểu sử")]
        public string? Bio { get; set; }

        [Display(Name = "Website")]
        [Url]
        public string? Website { get; set; }

        public string? ExistingImageUrl { get; set; }

        [Display(Name = "Thay đổi ảnh đại diện")]
        public IFormFile? AvatarImage { get; set; }
    }
}