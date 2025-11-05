using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Publisher
{
    public class PublisherViewModel
    {
        public int PublisherId { get; set; }
        
        [Display(Name = "Tên nhà xuất bản")]
        public string Name { get; set; } = null!;
        
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }
        
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
