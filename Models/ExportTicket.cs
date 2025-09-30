using System;
using System.Collections.Generic;

namespace BookstoreManagement.Models;

public partial class ExportTicket
{
    public int ExportId { get; set; }

    public required string UserId { get; set; }

    public int? ReferenceId { get; set; }

    public DateTime? Date { get; set; }

    public int? TotalQuantity { get; set; }

    public string Status { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public string? DocumentNumber { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ExportDetail> ExportDetails { get; set; } = new List<ExportDetail>();

    public virtual Order? Reference { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
