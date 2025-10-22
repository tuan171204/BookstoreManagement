using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Author
{
    public class AuthorViewModel
    {
        public int AuthorId { get; set; }
        
        [Display(Name = "Tên tác giả")]
        public string Name { get; set; } = null!;
        
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
