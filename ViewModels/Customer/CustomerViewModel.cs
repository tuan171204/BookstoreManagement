using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Customer
{
    public class CustomerViewModel
    {
        public string? CustomerId { get; set; }

        [Required(ErrorMessage = "Họ tên không được bỏ trống")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email không được bỏ trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được bỏ trống")]
        public required string Phone { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }
        public List<BookstoreManagement.Models.Order>? Orders { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
