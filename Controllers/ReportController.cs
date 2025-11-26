using BookstoreManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookstoreManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ReportController : Controller
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            TempData["CurrentFeature"] = "Report"; // để sidebar active đúng

            // Dữ liệu cho các card tổng quan + bảng top sách
            ViewBag.MonthSummary   = await _reportService.GetCurrentMonthSummaryAsync();
            ViewBag.TodaySummary = await _reportService.GetTodaySummaryAsync();
            ViewBag.MonthRevenue = await _reportService.GetCurrentMonthRevenueAsync();
            ViewBag.TopBooks     = await _reportService.GetTopBestSellersAsync(10);
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

            // Đảm bảo JSON trả về đúng định dạng: [{ label: "01/2025", revenue: 125000000 }, ...]
            return Json(data.Select(x => new
            {
                label = ((dynamic)x).Label,
                revenue = ((dynamic)x).Revenue
            }));
        }
    }
}