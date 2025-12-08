using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Publisher
{
    public class PublisherEditViewModel
    {
        public int PublisherId { get; set; }

        [Required(ErrorMessage = "Tên nhà xuất bản là bắt buộc")]
        [Display(Name = "Tên nhà xuất bản")]
        public string Name { get; set; } = null!;

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public string? ExistingLogoUrl { get; set; }

        [Display(Name = "Thay đổi Logo")]
        public IFormFile? LogoImage { get; set; }
    }
}