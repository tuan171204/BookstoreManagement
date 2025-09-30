using System;
using System.Collections.Generic;

namespace BookstoreManagement.Models;

public partial class SupplierBook
{
    public int SupplierId { get; set; }

    public int BookId { get; set; }

    public decimal? DefaultCostPrice { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;
}
