using System;
using System.Collections.Generic;

namespace BookstoreManagement.Models;

public partial class Code
{
    public int CodeId { get; set; }

    public string Entity { get; set; } = null!;

    public int Key { get; set; }

    public string Value { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ImportTicket> ImportTickets { get; set; } = new List<ImportTicket>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
}
