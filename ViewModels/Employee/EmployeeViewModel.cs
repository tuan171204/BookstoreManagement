using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Employee
{
    public class EmployeeViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Họ tên không được bỏ trống")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email không được bỏ trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được bỏ trống")]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chức vụ")]
        public string? RoleName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}