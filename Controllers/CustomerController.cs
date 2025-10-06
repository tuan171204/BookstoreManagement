using Microsoft.EntityFrameworkCore;
using BookstoreManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookstoreManagement.Controllers
{
    public class CustomerController : Controller
    {
        private readonly BookstoreContext _context;

        public CustomerController(BookstoreContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            TempData["CurrentFeature"] = "Customer";
            var customers = await _context.Customers.ToListAsync();
            return View(customers);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            // Validate dữ liệu theo các Attribute trong Customer Model, nếu không hợp lệ thì trả về _CreateForm cùng lõi
            if (!ModelState.IsValid)
                return PartialView("_CreateForm", customer);

            customer.CustomerId = Guid.NewGuid().ToString();
            customer.CreatedAt = DateTime.Now;
            customer.IsActive = true;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thêm khách hàng thành công!" });
        }
    }
}