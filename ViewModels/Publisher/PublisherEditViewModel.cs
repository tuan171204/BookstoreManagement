using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Publisher
{
    public class PublisherEditViewModel
    {
        public int PublisherId { get; set; }
        
        [Required(ErrorMessage = "Tên nhà xuất bản là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên nhà xuất bản không được vượt quá 100 ký tự")]
        [Display(Name = "Tên nhà xuất bản")]
        public string Name { get; set; } = null!;
        
        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        [Display(Name = "Địa chỉ")]
        [DataType(DataType.MultilineText)]
        public string? Address { get; set; }
    }
}
