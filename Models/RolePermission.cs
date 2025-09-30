using System;
using System.Collections.Generic;

namespace BookstoreManagement.Models;

public partial class RolePermission
{
    public required string RoleId { get; set; }

    public int PermissionId { get; set; }

    public virtual AppRole Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;

}
