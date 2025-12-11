using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Employee
{
    public class EmployeeViewModel
    {
        public int Id { get; set; } // Map với EmployeeId (int)

        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Họ tên không được bỏ trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
        public string FullName { get; set; } = null!;

        [Display(Name = "Email liên hệ")]
        [Required(ErrorMessage = "Email không được bỏ trống")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        public string? Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Số điện thoại không được bỏ trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = null!;

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Ngày tuyển dụng")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; } = DateTime.Now;

        [Display(Name = "Lương cơ bản")]
        [DataType(DataType.Currency)]
        public decimal Salary { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;

        // Trường này để hiển thị xem nhân viên đã có tài khoản hay chưa (dùng cho View)
        public string? AccountId { get; set; }
        public string? AccountUsername { get; set; }
    }
}