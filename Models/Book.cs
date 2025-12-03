using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class Book
{
    public int BookId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string Title { get; set; } = null!;

    [Column(TypeName = "nvarchar(255)")]
    public string? ImageUrl { get; set; }

    public int AuthorId { get; set; }

    public int PublisherId { get; set; }

    public int? PublicationYear { get; set; }

    public decimal Price { get; set; }

    public int? StockQuantity { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

    public int? LowStockThreshold { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Author Author { get; set; } = null!;

    public virtual ICollection<ExportDetail> ExportDetails { get; set; } = new List<ExportDetail>();

    public virtual ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual Publisher Publisher { get; set; } = null!;

    public virtual ICollection<SupplierBook> SupplierBooks { get; set; } = new List<SupplierBook>();
}
