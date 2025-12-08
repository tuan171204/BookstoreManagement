using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Publisher
{
    public class PublisherCreateViewModel
    {
        [Required(ErrorMessage = "Tên nhà xuất bản là bắt buộc")]
        [Display(Name = "Tên nhà xuất bản")]
        public string Name { get; set; } = null!;

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "SĐT không hợp lệ")]
        public string? Phone { get; set; }

        [Display(Name = "Email liên hệ")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Display(Name = "Website")]
        [Url(ErrorMessage = "Website không hợp lệ")]
        public string? Website { get; set; }

        [Display(Name = "Mô tả / Giới thiệu")]
        public string? Description { get; set; }

        [Display(Name = "Logo Nhà xuất bản")]
        public IFormFile? LogoImage { get; set; }
    }
}