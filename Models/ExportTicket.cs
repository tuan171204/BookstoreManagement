using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class ExportTicket
{
    public int ExportId { get; set; }

    public required string UserId { get; set; }

    public int? ReferenceId { get; set; }

    public DateTime? Date { get; set; }

    public int? TotalQuantity { get; set; }

[Column(TypeName = "nvarchar(100)")]
    public string Status { get; set; } = null!;

[Column(TypeName = "nvarchar(100)")]
    public string Reason { get; set; } = null!;

    public string? DocumentNumber { get; set; }

[Column(TypeName = "nvarchar(255)")]
    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ExportDetail> ExportDetails { get; set; } = new List<ExportDetail>();

    public virtual Order? Reference { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
