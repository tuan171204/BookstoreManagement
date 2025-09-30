using System;
using System.Collections.Generic;

namespace BookstoreManagement.Models;

public partial class ExportDetail
{
    public int ExportDetailId { get; set; }

    public int ExportId { get; set; }

    public int BookId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? Subtotal { get; set; }

    public string? Note { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual ExportTicket Export { get; set; } = null!;
}
