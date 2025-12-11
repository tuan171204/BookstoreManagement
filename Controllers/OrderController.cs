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
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string? status, int pageNumber = 1, int pageSize = 10)
        {
            TempData["CurrentFeature"] = "Order";

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

            // Tính tổng số items trước khi phân trang
            var totalItems = await ordersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Phân trang và sắp xếp
            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
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
    }
}