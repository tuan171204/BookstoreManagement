using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class ExportDetail
{
    public int ExportDetailId { get; set; }

    public int ExportId { get; set; }

    public int BookId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? Subtotal { get; set; }

    [Column(TypeName = "nvarchar(255)")]
    public string? Note { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual ExportTicket Export { get; set; } = null!;
}
