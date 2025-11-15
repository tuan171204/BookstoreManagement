using BookstoreManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookstoreManagement.ViewModels.Warehouse
{
    // ViewModel cho trang Index (Danh sách)
    public class WarehouseTicketViewModel
    {
        public int Id { get; set; }

        // "Import" hoặc "Export"
        public string Type { get; set; } = null!;

        // Mã phiếu: PN001 hoặc PX001
        public string? DocumentNumber { get; set; }

        public DateTime? Date { get; set; }

        // NCC (cho phiếu nhập) hoặc Lý do (cho phiếu xuất)
        public string Reference { get; set; } = null!;

        public int? TotalQuantity { get; set; }

        // Chỉ phiếu nhập mới có
        public decimal? TotalCost { get; set; }

        public string Status { get; set; } = null!;
    }

    // ViewModel cho trang Detail
    public class WarehouseDetailViewModel
    {
        // "Import" hoặc "Export"
        public string TicketType { get; set; } = "Import";

        // Chỉ 1 trong 2 cái này có dữ liệu
        public ImportTicket? ImportTicket { get; set; }
        public ExportTicket? ExportTicket { get; set; }

        // Dùng cho title
        public string PageTitle => TicketType == "Import" ? "Chi tiết Phiếu Nhập" : "Chi tiết Phiếu Xuất";
    }
}