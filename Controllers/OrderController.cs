using BookstoreManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BookstoreManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager")] // Chỉ Admin và Quản lý mới được xem/xóa
    public class OrderController : Controller
    {
        private readonly BookstoreContext _context;

        public OrderController(BookstoreContext context)
        {
            _context = context;
        }

        // GET: /Order (Danh sách hóa đơn)
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string? status, string sortBy = "OrderDate", string sortOrder = "desc", int pageNumber = 1, int pageSize = 10)
        {
            TempData["CurrentFeature"] = "Order";
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;
            if (fromDate.HasValue) ViewData["FromDateFilter"] = fromDate.Value.ToString("yyyy-MM-dd");
            if (toDate.HasValue) ViewData["ToDateFilter"] = toDate.Value.ToString("yyyy-MM-dd");
            if (!string.IsNullOrEmpty(status)) ViewData["StatusFilter"] = status;

            // Tạo query cơ bản
            var ordersQuery = _context.Orders
                .Include(o => o.Customer)     
                .Include(o => o.User)          
                .Include(o => o.PaymentMethod)
                .AsQueryable();

            // Filter theo khoảng thời gian
            if (fromDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                // Thêm 1 ngày và trừ 1 tick để lấy cả ngày cuối
                var endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= endDate);
            }

            // Filter theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            // Apply sorting
            ordersQuery = sortBy?.ToLower() switch
            {
                "orderid" => sortOrder == "asc" 
                    ? ordersQuery.OrderBy(o => o.OrderId) 
                    : ordersQuery.OrderByDescending(o => o.OrderId),
                "orderdate" => sortOrder == "asc" 
                    ? ordersQuery.OrderBy(o => o.OrderDate) 
                    : ordersQuery.OrderByDescending(o => o.OrderDate),
                "customer" => sortOrder == "asc" 
                    ? ordersQuery.OrderBy(o => o.Customer.FullName) 
                    : ordersQuery.OrderByDescending(o => o.Customer.FullName),
                "user" => sortOrder == "asc" 
                    ? ordersQuery.OrderBy(o => o.User.FullName) 
                    : ordersQuery.OrderByDescending(o => o.User.FullName),
                "finalamount" => sortOrder == "asc" 
                    ? ordersQuery.OrderBy(o => o.FinalAmount) 
                    : ordersQuery.OrderByDescending(o => o.FinalAmount),
                "status" => sortOrder == "asc" 
                    ? ordersQuery.OrderBy(o => o.Status) 
                    : ordersQuery.OrderByDescending(o => o.Status),
                _ => ordersQuery.OrderByDescending(o => o.OrderDate)
            };

            // Tính tổng số items trước khi phân trang
            var totalItems = await ordersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Phân trang và sắp xếp
            var orders = await ordersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Tạo SelectList cho Status filter
            var statusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Tất cả --", Selected = string.IsNullOrEmpty(status) }
            };

            // Lấy các status có trong database
            var distinctStatuses = await _context.Orders
                .Select(o => o.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            foreach (var stat in distinctStatuses)
            {
                statusList.Add(new SelectListItem 
                { 
                    Value = stat, 
                    Text = stat, 
                    Selected = status == stat 
                });
            }

            ViewBag.StatusFilter = new SelectList(statusList, "Value", "Text", status);
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedStatus = status;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.FromDateParam = fromDate;
            ViewBag.ToDateParam = toDate;
            ViewBag.StatusParam = status;

            return View(orders);
        }

        // GET: /Order/Details/5 (Xem chi tiết hóa đơn)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartShipping(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Chỉ đơn hàng đang chờ xử lý mới có thể chuyển sang giao hàng.";
            }
            else
            {
                order.Status = "Shipping"; // Trạng thái mới: Đang giao hàng
                order.UpdatedAt = DateTime.Now;
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã chuyển trạng thái sang Đang giao hàng!";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        

        // 3. Action Hủy (Giữ nguyên, chỉ cho hủy khi Pending)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status == "Pending")
            {
                // Lưu ý: Nếu muốn hoàn kho thì viết code cộng lại StockQuantity ở đây
                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.Now;
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã hủy đơn hàng.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng đã giao hoặc đã hoàn thành.";
            }
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}