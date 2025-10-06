using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class Customer
{
    public string? CustomerId { get; set; }

    [Required(ErrorMessage = "Họ tên không được bỏ trống")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Họ tên 6 - 100 ký tự")]
    [Column(TypeName = "nvarchar(100)")]
    public string FullName { get; set; } = null!;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public required string Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [Column(TypeName = "nvarchar(255)")]
    public string? Address { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
