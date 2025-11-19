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
                .Include(o => o.Customer)      // Lấy thông tin Khách hàng
                .Include(o => o.User)          // Lấy thông tin Nhân viên bán
                .Include(o => o.PaymentMethod) // Lấy phương thức thanh toán
                .OrderByDescending(o => o.OrderDate) // Mới nhất lên đầu
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
                .Include(o => o.OrderDetails)       // Lấy danh sách chi tiết
                    .ThenInclude(od => od.Book)     // Lấy thông tin Sách trong chi tiết
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}