using BookstoreManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> Index()
        {
            TempData["CurrentFeature"] = "Order";
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

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

        // 2. Action: Chuyển từ "Shipping" -> "Completed"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != "Shipping")
            {
                TempData["ErrorMessage"] = "Chỉ đơn hàng đang giao mới có thể xác nhận hoàn thành.";
            }
            else
            {
                order.Status = "Completed"; // Trạng thái cuối: Hoàn thành
                order.UpdatedAt = DateTime.Now;
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đơn hàng đã hoàn thành!";
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