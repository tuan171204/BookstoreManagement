using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class ImportTicket
{
    public int ImportId { get; set; }

    public required string UserId { get; set; }

    public int SupplierId { get; set; }

    public DateTime? Date { get; set; }

    public int? TotalQuantity { get; set; }

    public decimal? TotalCost { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string Status { get; set; } = null!;

    public int? PaymentMethodId { get; set; }

    public string? DocumentNumber { get; set; }

    [Column(TypeName = "nvarchar(255)")]
    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();

    public virtual Code? PaymentMethod { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}
