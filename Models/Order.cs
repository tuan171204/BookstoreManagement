using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string? CustomerId { get; set; }

    public required string UserId { get; set; }

    public int? PromotionId { get; set; }

    public int PaymentMethodId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal FinalAmount { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<ExportTicket> ExportTickets { get; set; } = new List<ExportTicket>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Code PaymentMethod { get; set; } = null!;

    public virtual Promotion? Promotion { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
