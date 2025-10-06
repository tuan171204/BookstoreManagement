using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BookstoreManagement.Models;

// Custom ASP.NET Identity User class
public partial class AppUser : IdentityUser
{
    [Column(TypeName = "nvarchar(100)")]
    public string? FullName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<ExportTicket> ExportTickets { get; set; } = new List<ExportTicket>();
    public virtual ICollection<ImportTicket> ImportTickets { get; set; } = new List<ImportTicket>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

