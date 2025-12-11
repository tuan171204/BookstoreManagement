using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models
{
    // Bảng lưu lịch sử biến động giá để báo cáo lợi nhuận chính xác theo thời gian
    public class BookPriceHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int BookId { get; set; }

        // Giá nhập tại thời điểm đó
        [Column(TypeName = "decimal(18, 2)")]
        public decimal CostPrice { get; set; }

        // % Lợi nhuận tại thời điểm đó
        public double ProfitMargin { get; set; }

        // Giá bán tại thời điểm đó
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SellingPrice { get; set; }

        // Ngày bắt đầu áp dụng mức giá này
        public DateTime EffectiveDate { get; set; } = DateTime.Now;

        // Người thực hiện thay đổi giá (Optional - để biết ai sửa giá)
        public string? UpdatedBy { get; set; }

        // Navigation Property
        [ForeignKey("BookId")]
        public virtual Book Book { get; set; } = null!;
    }
}