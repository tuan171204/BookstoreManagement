using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class Code
{
    public int CodeId { get; set; }

    public string Entity { get; set; } = null!;

    public int Key { get; set; }

[Column(TypeName = "nvarchar(100)")]
    public string Value { get; set; } = null!;

[Column(TypeName = "nvarchar(255)")]
    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ImportTicket> ImportTickets { get; set; } = new List<ImportTicket>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
}
