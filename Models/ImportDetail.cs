using System;
using System.Collections.Generic;

namespace BookstoreManagement.Models;

public partial class ImportDetail
{
    public int ImportDetailId { get; set; }

    public int ImportId { get; set; }

    public int BookId { get; set; }

    public int Quantity { get; set; }

    public decimal CostPrice { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? Discount { get; set; }

    public string? Note { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual ImportTicket Import { get; set; } = null!;
}
