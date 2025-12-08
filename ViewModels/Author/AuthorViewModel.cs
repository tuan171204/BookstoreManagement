using System;
using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Author
{
    public class AuthorViewModel
    {
        public int AuthorId { get; set; }

        [Display(Name = "Tên tác giả")]
        public string Name { get; set; } = null!;

        [Display(Name = "Bút danh")]
        public string? Pseudonym { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Quốc tịch")]
        public string? Nationality { get; set; }

        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Tiểu sử")]
        public string? Bio { get; set; }

        [Display(Name = "Số lượng sách")]
        public int BooksCount { get; set; }

        [Display(Name = "Ngày tạo")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Ngày cập nhật")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? UpdatedAt { get; set; }
    }
}