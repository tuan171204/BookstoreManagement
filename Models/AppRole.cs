using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace BookstoreManagement.Models;

// Custom ASP.NET Identity Role class
public partial class AppRole : IdentityRole
{
    public string? Description { get; set; }
    public decimal Salary { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}