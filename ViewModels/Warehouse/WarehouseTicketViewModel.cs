using BookstoreManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookstoreManagement.ViewModels.Warehouse
{
    
    public class WarehouseTicketViewModel
    {
        public int Id { get; set; }

        
        public string Type { get; set; } = null!;

        
        public string? DocumentNumber { get; set; }

        public DateTime? Date { get; set; }

        
        public string Reference { get; set; } = null!;

        public int? TotalQuantity { get; set; }

     
        public decimal? TotalCost { get; set; }

        public string Status { get; set; } = null!;
    }


    public class WarehouseDetailViewModel
    {
      
        public string TicketType { get; set; } = "Import";

      
        public ImportTicket? ImportTicket { get; set; }
        public ExportTicket? ExportTicket { get; set; }


        public string PageTitle => TicketType == "Import" ? "Chi tiết Phiếu Nhập" : "Chi tiết Phiếu Xuất";
    }
}