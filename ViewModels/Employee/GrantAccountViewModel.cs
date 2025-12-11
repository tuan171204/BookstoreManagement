using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Employee
{
    public class GrantAccountViewModel
    {
        public int EmployeeId { get; set; }

        [Display(Name = "Tên nhân viên")]
        public string? EmployeeName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email đăng nhập")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email (Tài khoản)")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự")]
        [Display(Name = "Mật khẩu cấp")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn quyền hạn")]
        [Display(Name = "Phân quyền (Role)")]
        public string RoleName { get; set; } = null!;
    }
}