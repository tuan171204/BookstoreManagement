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

    // --- CÁC TRƯỜNG GIÁ CẢ MỚI ---

    // 1. Giá nhập (Giá vốn) hiện tại
    [Column(TypeName = "decimal(18, 2)")]
    public decimal CostPrice { get; set; } = 0;

    // 2. % Lợi nhuận mong muốn (VD: 20 nghĩa là 20%)
    public double ProfitMargin { get; set; } = 0;

    // 3. Giá bán (Price): Sẽ được tính = CostPrice * (1 + ProfitMargin/100)
    // Giữ nguyên cột này để không bị lỗi code cũ, nhưng logic tính sẽ thay đổi
    public decimal Price { get; set; }

    public int? StockQuantity { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

    public int? LowStockThreshold { get; set; }

    public double AverageRating { get; set; } = 0; // Điểm trung bình (ví dụ: 4.5)

    public int TotalRatings { get; set; } = 0;     // Tổng số lượt đánh giá

    // Quan hệ với bảng Rating
    public virtual ICollection<BookRating> BookRatings { get; set; } = new List<BookRating>();

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

    public virtual ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();

    // Thêm quan hệ với bảng lịch sử giá
    public virtual ICollection<BookPriceHistory> PriceHistories { get; set; } = new List<BookPriceHistory>();

}
