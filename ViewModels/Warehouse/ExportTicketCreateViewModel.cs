using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Warehouse
{
    public class ExportDetailItem
    {
        [Required]
        public int BookId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        // Giá xuất (có thể là giá bìa hoặc 0 tùy lý do)
        public decimal UnitPrice { get; set; }
    }

    public class ExportTicketCreateViewModel
    {
        // Lý do xuất (Hỏng, Trả hàng, ...)
        [Required(ErrorMessage = "Vui lòng nhập lý do xuất kho")]
        [MaxLength(100)]
        public string Reason { get; set; } = null!;

        [MaxLength(255)]
        public string? Note { get; set; }

        // Danh sách chi tiết
        public List<ExportDetailItem> Details { get; set; } = new List<ExportDetailItem>();

        // Dữ liệu cho Dropdown Sách
        public SelectList? Books { get; set; }
    }
}