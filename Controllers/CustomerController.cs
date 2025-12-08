using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookstoreManagement.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly BookstoreContext _context;

        public CustomerController(BookstoreContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            TempData["CurrentFeature"] = "Customer"; 
            var query = _context.Customers.AsQueryable();

            query = query.Where(c => c.Phone != "0000000000");

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.FullName.Contains(searchString)
                                      || u.Email.Contains(searchString)
                                      || u.Phone.Contains(searchString));
            }

            var customers = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(customers);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.Orders)
                    .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(m => m.CustomerId == id);

            if (customer == null) return NotFound();

            var viewModel = new CustomerViewModel
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                IsActive = customer.IsActive,

                Orders = customer.Orders.OrderByDescending(o => o.OrderDate).ToList()
            };

            return View(viewModel);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            Console.WriteLine("Gọi đến action Create trong CustomerController ... ");
            if (ModelState.IsValid)
            {
                var customer = new Customer
                {
                    CustomerId = Guid.NewGuid().ToString(),
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Email = model.Email,
                    Address = model.Address,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                Console.WriteLine("Khách hàng mới: ", customer.ToString());

                _context.Add(customer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm khách hàng thành công!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"Lỗi xác thực: {error.ErrorMessage}");
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            var viewModel = new CustomerViewModel
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                IsActive = customer.IsActive
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, CustomerViewModel model)
        {
            if (id != model.CustomerId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var customer = await _context.Customers.FindAsync(id);
                    if (customer == null) return NotFound();

                    customer.FullName = model.FullName;
                    customer.Phone = model.Phone;
                    customer.Email = model.Email;
                    customer.Address = model.Address;
                    customer.IsActive = model.IsActive;
                    customer.UpdatedAt = DateTime.Now;

                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(model.CustomerId)) return NotFound();
                    else throw;
                }

                TempData["SuccessMessage"] = "Cập nhật khách hàng thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                customer.IsActive = false;
                customer.UpdatedAt = DateTime.Now;
                _context.Update(customer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã xóa (khóa) khách hàng thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(string id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}