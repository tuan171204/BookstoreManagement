using System;

namespace BookstoreManagement.ViewModels.Report
{
    public class ReportViewModel
    {
        // Bộ lọc
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Chỉ số kinh doanh (Profit & Loss)
        public int TotalOrders { get; set; }          // Tổng đơn hàng
        public int ProductsSold { get; set; }         // Tổng số cuốn sách bán được
        public decimal Revenue { get; set; }          // Doanh thu (Tiền bán hàng thực tế)
        public decimal COGS { get; set; }             // Giá vốn hàng bán (Chi phí gốc của sách đã bán)
        public decimal GrossProfit => Revenue - COGS; // Lợi nhuận gộp
        public decimal ProfitMargin => Revenue > 0 ? (GrossProfit / Revenue) * 100 : 0; // % Lợi nhuận

        // Chỉ số dòng tiền (Cashflow - Tham khảo)
        public decimal TotalImportCost { get; set; }  // Tiền chi ra nhập hàng trong kỳ

        // Biểu đồ
        public string ChartLabels { get; set; } = "[]";
        public string ChartRevenueData { get; set; } = "[]";
        public string ChartProfitData { get; set; } = "[]";
    }
}