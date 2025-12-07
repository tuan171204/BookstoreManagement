using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class Supplier
{
    public int SupplierId { get; set; }
    [Column(TypeName = "nvarchar(100)")]
    public string Name { get; set; } = null!;
    [Column(TypeName = "nvarchar(255)")]
    public string? ContactInfo { get; set; }
    [Column(TypeName = "nvarchar(255)")]
    public string? Address { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<ImportTicket> ImportTickets { get; set; } = new List<ImportTicket>();

    public virtual ICollection<SupplierBook> SupplierBooks { get; set; } = new List<SupplierBook>();


}
