using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BookstoreManagement.Models;

// Custom ASP.NET Identity User class
public partial class AppUser : IdentityUser
{
    [Required(ErrorMessage = "Họ tên không được bỏ trống")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Họ tên 6 - 100 ký tự")]
    [Column(TypeName = "nvarchar(100)")]
    public string? FullName { get; set; }

    [Column(TypeName = "nvarchar(255)")]
    public string? Address { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsDefaultPassword { get; set; } = true;

    public virtual ICollection<ExportTicket> ExportTickets { get; set; } = new List<ExportTicket>();
    public virtual ICollection<ImportTicket> ImportTickets { get; set; } = new List<ImportTicket>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<BookRating> BookRatings { get; set; } = new List<BookRating>();
}

