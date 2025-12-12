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

    public virtual ICollection<BookPromotion> BookPromotions { get; set; } = new List<BookPromotion>();

    public virtual Code Type { get; set; } = null!;

    // Thêm trường này: "All", "Online", "InStore"
    [Column(TypeName = "nvarchar(20)")]
    public string ApplyChannel { get; set; } = "All";

    // --- THÊM TRƯỜNG NÀY ---
    // Values: "Specific" (Sách chỉ định), "All" (Toàn bộ sách), "Order" (Tổng hóa đơn)
    [Column(TypeName = "nvarchar(20)")]
    public string ApplyType { get; set; } = "Specific";
}
