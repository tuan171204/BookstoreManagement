using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Report;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookstoreManagement.Services
{
    public class ReportService
    {
        private readonly BookstoreContext _context;

        public ReportService(BookstoreContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetRevenueAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.Orders
                .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate && o.Status == "Completed")
                .SumAsync(o => o.FinalAmount);
        }

        public async Task<decimal> GetCurrentMonthRevenueAsync()
        {
            var now = DateTime.Now;
            var firstDay = new DateTime(now.Year, now.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            var revenue = await GetRevenueAsync(firstDay, lastDay);
            var cost = await _context.ImportTickets
                .Where(i => i.Date >= firstDay && i.Date <= lastDay && i.Status == "Completed")
                .SumAsync(i => i.TotalCost ?? 0m);
            return revenue - cost;
        }

        public async Task<object> GetCurrentMonthSummaryAsync()
        {
            var now = DateTime.Now;
            var firstDay = new DateTime(now.Year, now.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

            var orders = await _context.Orders
                .Where(o => o.OrderDate >= firstDay && o.OrderDate <= lastDay && o.Status == "Completed")
                .ToListAsync();

            var revenue = orders.Sum(o => o.FinalAmount);
            var cost = await _context.ImportTickets
                .Where(i => i.Date >= firstDay && i.Date <= lastDay && i.Status == "Completed")
                .SumAsync(i => i.TotalCost ?? 0m);

            return new
            {
                SoDonHang = orders.Count,
                DoanhThu = revenue,
                ChiPhiNhap = cost,
                LoiNhuan = revenue - cost
            };
        }

        public async Task<List<object>> GetTopBestSellersAsync(int top = 10)
        {
            return await _context.OrderDetails
                .Include(od => od.Book)
                .Include(od => od.Order)
                .Where(od => od.Order.Status == "Completed")
                .GroupBy(od => new { od.Book.BookId, od.Book.Title })
                .Select(g => new
                {
                    BookId = g.Key.BookId,
                    Title = g.Key.Title,
                    SoLuong = g.Sum(x => x.Quantity),
                    DoanhThu = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(top)
                .Cast<object>()
                .ToListAsync();
        }

        public async Task<List<object>> GetRevenueLast12MonthsAsync()
        {
            var result = new List<object>();
            var today = DateTime.Today;

            for (int i = 11; i >= 0; i--)
            {
                var date = today.AddMonths(-i);
                var firstDay = new DateTime(date.Year, date.Month, 1);
                var lastDay = firstDay.AddMonths(1).AddDays(-1);

                var revenue = await GetRevenueAsync(firstDay, lastDay);
                var cost = await _context.ImportTickets
                    .Where(i => i.Date >= firstDay && i.Date <= lastDay && i.Status == "Completed")
                    .SumAsync(i => i.TotalCost ?? 0m);

                result.Add(new
                {
                    Month = date.ToString("MM/yyyy"),
                    Revenue = revenue,
                    Cost = cost,
                    Profit = revenue - cost
                });
            }

            return result;
        }

        public async Task<List<object>> GetOrderStatusSummaryAsync()
        {
            return await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key ?? "Chưa xác định",
                    Count = g.Count()
                })
                .Cast<object>()
                .ToListAsync();
        }

        public async Task<object> GetTodaySummaryAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var orders = await _context.Orders
                .Where(o => o.OrderDate >= today && o.OrderDate < tomorrow && o.Status == "Completed")
                .ToListAsync();

            var revenue = orders.Sum(o => o.FinalAmount);
            var cost = await _context.ImportTickets
                .Where(i => i.Date >= today && i.Date <= tomorrow.AddDays(-1) && i.Status == "Completed")
                .SumAsync(i => i.TotalCost ?? 0m);

            return new
            {
                SoDonHang = orders.Count,
                DoanhThu = revenue,
                ChiPhiNhap = cost,
                LoiNhuan = revenue - cost
            };
        }

        public async Task<List<object>> GetRevenueByDateRangeAsync(
            DateTime fromDate,
            DateTime toDate,
            string groupBy = "day")
        {
            toDate = toDate.Date.AddDays(1).AddTicks(-1);

            // Doanh thu bán – FinalAmount là decimal (không nullable) → không cần ?? 0m
            var sales = await _context.Orders
                .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate && o.Status == "Completed")
                .Select(o => new { o.OrderDate, FinalAmount = o.FinalAmount })
                .ToListAsync();

            // Chi phí nhập – TotalCost nullable → dùng ?? 0m
            var imports = await _context.ImportTickets
                .Where(i => i.Date >= fromDate && i.Date <= toDate && i.Status == "Completed")
                .Select(i => new { OrderDate = i.Date, Amount = i.TotalCost ?? 0m })
                .ToListAsync();

            // Gom nhóm doanh thu
            var salesGrouped = groupBy?.ToLower() switch
            {
                "day" => sales.GroupBy(o => o.OrderDate?.ToString("dd/MM/yyyy") ?? "N/A")
                              .Select(g => new { Label = g.Key, Revenue = g.Sum(x => x.FinalAmount) }),

                "month" => sales.GroupBy(o => o.OrderDate?.ToString("MM/yyyy") ?? "N/A")
                                .Select(g => new { Label = g.Key, Revenue = g.Sum(x => x.FinalAmount) }),

                "year" => sales.GroupBy(o => o.OrderDate?.Year ?? 0)
                               .Select(g => new { Label = g.Key == 0 ? "N/A" : g.Key.ToString(), Revenue = g.Sum(x => x.FinalAmount) }),

                _ => sales.GroupBy(o => o.OrderDate?.ToString("dd/MM/yyyy") ?? "N/A")
                          .Select(g => new { Label = g.Key, Revenue = g.Sum(x => x.FinalAmount) })
            };

            // Gom nhóm chi phí
            var costGrouped = groupBy?.ToLower() switch
            {
                "day" => imports.GroupBy(o => o.OrderDate?.ToString("dd/MM/yyyy") ?? "N/A")
                                .Select(g => new { Label = g.Key, Cost = g.Sum(x => x.Amount) }),

                "month" => imports.GroupBy(o => o.OrderDate?.ToString("MM/yyyy") ?? "N/A")
                                  .Select(g => new { Label = g.Key, Cost = g.Sum(x => x.Amount) }),

                "year" => imports.GroupBy(o => o.OrderDate?.Year ?? 0)
                                 .Select(g => new { Label = g.Key == 0 ? "N/A" : g.Key.ToString(), Cost = g.Sum(x => x.Amount) }),

                _ => imports.GroupBy(o => o.OrderDate?.ToString("dd/MM/yyyy") ?? "N/A")
                            .Select(g => new { Label = g.Key, Cost = g.Sum(x => x.Amount) })
            };

            var allLabels = salesGrouped.Select(g => g.Label)
                                        .Union(costGrouped.Select(g => g.Label))
                                        .Distinct()
                                        .OrderBy(l => l);

            var result = allLabels.Select(label =>
            {
                var revenue = salesGrouped.FirstOrDefault(g => g.Label == label)?.Revenue ?? 0m;
                var cost = costGrouped.FirstOrDefault(g => g.Label == label)?.Cost ?? 0m;
                var profit = revenue - cost;

                return new
                {
                    Label = label,
                    Revenue = revenue,
                    Cost = cost,
                    Profit = profit
                };
            }).Cast<object>().ToList();

            return result;
        }

        public async Task<ReportViewModel> GetReportDataAsync(DateTime fromDate, DateTime toDate)
        {
            // Chuẩn hóa thời gian: Từ 00:00:00 ngày đầu đến 23:59:59 ngày cuối
            var start = fromDate.Date;
            var end = toDate.Date.AddDays(1).AddSeconds(-1);

            // 1. Lấy danh sách đơn hàng hoàn thành trong kỳ
            // Include OrderDetails và Book để tính giá vốn
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .Where(o => o.OrderDate >= start && o.OrderDate <= end && o.Status == "Completed")
                .ToListAsync();

            // 2. Tính toán các chỉ số
            int totalOrders = orders.Count;
            decimal revenue = orders.Sum(o => o.FinalAmount);

            // Tính tổng số lượng sách bán
            int productsSold = orders.SelectMany(o => o.OrderDetails).Sum(od => od.Quantity);

            // Tính Giá vốn hàng bán (COGS)
            // Logic: Số lượng bán * Giá vốn (CostPrice) của sách đó
            decimal cogs = orders.SelectMany(o => o.OrderDetails)
                                 .Sum(od => od.Quantity * od.Book.CostPrice);

            // 3. Tính chi phí nhập hàng (Cashflow Out) - Để tham khảo
            decimal importCost = await _context.ImportTickets
                .Where(i => i.Date >= start && i.Date <= end && i.Status == "Completed")
                .SumAsync(i => i.TotalCost ?? 0);

            // 4. Chuẩn bị dữ liệu biểu đồ (Group theo ngày)
            // Gom nhóm orders theo ngày để vẽ biểu đồ
            var chartData = orders
                .GroupBy(o => o.OrderDate.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    DailyRevenue = g.Sum(x => x.FinalAmount),
                    // Tính lợi nhuận ngày = Doanh thu ngày - Giá vốn ngày
                    DailyProfit = g.Sum(x => x.FinalAmount) - g.SelectMany(x => x.OrderDetails).Sum(od => od.Quantity * od.Book.CostPrice)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Nếu khoảng thời gian dài (> 31 ngày), gom theo tháng cho đỡ rối
            bool groupByMonth = (end - start).TotalDays > 31;

            // Xử lý labels và data cho ChartJS
            var labels = new List<string>();
            var revenueData = new List<decimal>();
            var profitData = new List<decimal>();

            if (groupByMonth)
            {
                var monthlyData = chartData
                    .GroupBy(x => new { x.Date.Year, x.Date.Month })
                    .Select(g => new
                    {
                        Label = $"{g.Key.Month}/{g.Key.Year}",
                        Revenue = g.Sum(x => x.DailyRevenue),
                        Profit = g.Sum(x => x.DailyProfit)
                    });

                foreach (var item in monthlyData)
                {
                    labels.Add(item.Label);
                    revenueData.Add(item.Revenue);
                    profitData.Add(item.Profit);
                }
            }
            else
            {
                // Lấp đầy các ngày trống (ngày không bán được hàng) để biểu đồ liền mạch
                for (var day = start; day <= end.Date; day = day.AddDays(1))
                {
                    labels.Add(day.ToString("dd/MM"));
                    var dataPoint = chartData.FirstOrDefault(x => x.Date == day);
                    revenueData.Add(dataPoint?.DailyRevenue ?? 0);
                    profitData.Add(dataPoint?.DailyProfit ?? 0);
                }
            }

            return new ReportViewModel
            {
                FromDate = start,
                ToDate = end,
                TotalOrders = totalOrders,
                ProductsSold = productsSold,
                Revenue = revenue,
                COGS = cogs,
                TotalImportCost = importCost,

                // Serialize dữ liệu biểu đồ sang JSON string để View dùng
                ChartLabels = System.Text.Json.JsonSerializer.Serialize(labels),
                ChartRevenueData = System.Text.Json.JsonSerializer.Serialize(revenueData),
                ChartProfitData = System.Text.Json.JsonSerializer.Serialize(profitData)
            };
        }
    }
}