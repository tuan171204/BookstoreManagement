using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class Promotion
{
    public int PromotionId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string Name { get; set; } = null!;

    public int TypeId { get; set; }

    public decimal? DiscountPercent { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public decimal? MinPurchaseAmount { get; set; }

    public int? GiftBookId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual Book? GiftBook { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Code Type { get; set; } = null!;
}
