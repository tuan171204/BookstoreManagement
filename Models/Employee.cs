using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Họ tên không được bỏ trống")]
    [Column(TypeName = "nvarchar(100)")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Column(TypeName = "varchar(20)")]
    public string PhoneNumber { get; set; } = null!;

    [EmailAddress]
    [Column(TypeName = "varchar(100)")]
    public string? Email { get; set; } // Email liên hệ, không nhất thiết là email đăng nhập

    [Column(TypeName = "nvarchar(255)")]
    public string? Address { get; set; }

    public DateTime HireDate { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Salary { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // --- LIÊN KẾT TÀI KHOẢN ---
    // Trường này sẽ Null nếu nhân viên chưa được cấp tài khoản
    public string? AccountId { get; set; }

    [ForeignKey("AccountId")]
    public virtual AppUser? AppUser { get; set; }
}