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
    }
}