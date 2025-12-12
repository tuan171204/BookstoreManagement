using Microsoft.EntityFrameworkCore;
using BookstoreManagement.Models;
using BookstoreManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BookstoreManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ReportController : Controller
    {
        private readonly ReportService _reportService;
        private readonly BookstoreContext _context;

        public ReportController(ReportService reportService, BookstoreContext context)
        {
            _reportService = reportService;
            _context = context;

        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            TempData["CurrentFeature"] = "Report";

            ViewBag.MonthSummary = await _reportService.GetCurrentMonthSummaryAsync();
            ViewBag.TodaySummary = await _reportService.GetTodaySummaryAsync();
            ViewBag.MonthRevenue = await _reportService.GetCurrentMonthRevenueAsync();
            ViewBag.TopBooks = await _reportService.GetTopBestSellersAsync(10);
            ViewBag.Last12Months = await _reportService.GetRevenueLast12MonthsAsync();

            return View();
        }

        /// <summary>
        /// API trả về dữ liệu doanh thu theo khoảng thời gian để vẽ biểu đồ Chart.js
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRevenueData(
            DateTime fromDate,
            DateTime toDate,
            string groupBy = "day")
        {
            // Nếu không truyền ngày → tự động lấy khoảng mặc định
            if (fromDate == default || toDate == default)
            {
                toDate = DateTime.Today;
                if (groupBy == "day")
                    fromDate = toDate.AddDays(-29);
                else if (groupBy == "month")
                    fromDate = toDate.AddMonths(-11);
                else // year
                    fromDate = new DateTime(toDate.Year - 9, 1, 1);
            }

            var data = await _reportService.GetRevenueByDateRangeAsync(fromDate, toDate, groupBy);

            return Json(data.Select(x => new
            {
                label = ((dynamic)x).Label,
                revenue = ((dynamic)x).Revenue
            }));
        }

        // =============================================================
        // THÊM API MỚI: THỐNG KÊ THEO DANH MỤC (CATEGORY)
        // =============================================================
        [HttpGet]
        public async Task<IActionResult> GetCategoryReport(DateTime? fromDate, DateTime? toDate)
        {
            // Mặc định lấy 30 ngày gần nhất nếu không chọn
            var to = toDate?.Date ?? DateTime.Today.AddDays(2);
            var from = fromDate?.Date ?? to.AddDays(-29);

            if (from > to) return BadRequest("Ngày bắt đầu phải nhỏ hơn ngày kết thúc");

            // Truy vấn dữ liệu
            // Lưu ý: Tồn kho (Stock) là tồn hiện tại (Snapshot), còn Nhập/Xuất tính theo khoảng thời gian
            var stats = await _context.Categories
                .Select(c => new
                {
                    Name = c.Name,
                    // Tính tổng nhập trong khoảng thời gian (chỉ tính phiếu đã hoàn thành)
                    ImportQty = c.BookCategories
                        .SelectMany(bc => bc.Book.ImportDetails)
                        .Where(d => d.Import.Date >= from && d.Import.Date <= to && d.Import.Status == "Completed")
                        .Sum(d => (int?)d.Quantity) ?? 0,

                    // Tính tổng xuất trong khoảng thời gian
                    ExportQty = c.BookCategories
                        .SelectMany(bc => bc.Book.ExportDetails)
                        .Where(d => d.Export.Date >= from && d.Export.Date <= to && d.Export.Status == "Completed")
                        .Sum(d => (int?)d.Quantity) ?? 0,

                    // Tính tồn kho HIỆN TẠI của các sách trong danh mục
                    StockQty = c.BookCategories.Sum(bc => (int?)bc.Book.StockQuantity) ?? 0
                })
                .Where(x => x.ImportQty > 0 || x.ExportQty > 0 || x.StockQty > 0) // Chỉ lấy danh mục có dữ liệu
                .ToListAsync();

            return Json(stats);
        }
    }
}