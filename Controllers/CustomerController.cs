using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookstoreManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class CustomerController : Controller
    {
        private readonly BookstoreContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CustomerController(BookstoreContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, bool? isActive, string sortBy = "CreatedAt", string sortOrder = "desc", int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;
            if (isActive.HasValue)
            {
                ViewData["IsActiveFilter"] = isActive.Value.ToString();
            }
            TempData["CurrentFeature"] = "Customer";

            var query = _context.Customers
                .Include(c => c.Rank)
                .Include(c => c.AppUser)
                .AsQueryable();

            // Loại bỏ khách hàng đặc biệt
            query = query.Where(c => c.Phone != "0000000000" && c.Phone != "00000000");

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.FullName.Contains(searchString)
                                      || u.Email.Contains(searchString)
                                      || u.Phone.Contains(searchString));
            }

            // Status filter
            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "fullname" => sortOrder == "asc" 
                    ? query.OrderBy(c => c.FullName) 
                    : query.OrderByDescending(c => c.FullName),
                "phone" => sortOrder == "asc" 
                    ? query.OrderBy(c => c.Phone) 
                    : query.OrderByDescending(c => c.Phone),
                "email" => sortOrder == "asc" 
                    ? query.OrderBy(c => c.Email ?? "") 
                    : query.OrderByDescending(c => c.Email ?? ""),
                "points" => sortOrder == "asc" 
                    ? query.OrderBy(c => c.Points) 
                    : query.OrderByDescending(c => c.Points),
                "rank" => sortOrder == "asc" 
                    ? query.OrderBy(c => c.Rank != null ? c.Rank.Value : "") 
                    : query.OrderByDescending(c => c.Rank != null ? c.Rank.Value : ""),
                "createdat" => sortOrder == "asc" 
                    ? query.OrderBy(c => c.CreatedAt) 
                    : query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderByDescending(c => c.CreatedAt)
            };

            // Calculate pagination
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Apply pagination
            var customers = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerViewModel
                {
                    CustomerId = c.CustomerId,
                    FullName = c.FullName,
                    Phone = c.Phone,
                    Email = c.Email,
                    Address = c.Address,
                    IsActive = c.IsActive,
                    Points = c.Points,
                    RankName = c.Rank != null ? c.Rank.Value : "Thành viên",
                    // Kiểm tra xem đã liên kết tài khoản chưa
                    HasAccount = c.AccountId != null
                })
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.IsActiveParam = isActive;

            return View(customers);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.Orders)
                    .ThenInclude(o => o.OrderDetails)
                .Include(c => c.Rank)
                .Include(c => c.AppUser)
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

                Points = customer.Points,
                RankName = customer.Rank?.Value ?? "Thành viên mới",

                HasAccount = customer.AccountId != null,

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
                if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại này đã tồn tại trong hệ thống.");
                    return View(model);
                }

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

                TempData["SuccessMessage"] = "Thêm hồ sơ khách hàng thành công!";
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

                    // Kiểm tra trùng SĐT nếu thay đổi
                    if (customer.Phone != model.Phone && await _context.Customers.AnyAsync(c => c.Phone == model.Phone))
                    {
                        ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng.");
                        return View(model);
                    }

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

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
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

        // Reset mật khẩu cho khách hàng (Admin)
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ResetPassword(string id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            if (customer.AccountId == null)
            {
                TempData["ErrorMessage"] = "Khách hàng này chưa có tài khoản đăng nhập.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.CustomerName = customer.FullName;
            ViewBag.CustomerId = customer.CustomerId;
            ViewBag.Email = customer.AppUser?.Email ?? customer.Email;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword, string confirmPassword)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            if (customer.AccountId == null)
            {
                TempData["ErrorMessage"] = "Khách hàng này chưa có tài khoản đăng nhập.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự.");
                ViewBag.CustomerName = customer.FullName;
                ViewBag.CustomerId = customer.CustomerId;
                ViewBag.Email = customer.AppUser?.Email ?? customer.Email;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                ViewBag.CustomerName = customer.FullName;
                ViewBag.CustomerId = customer.CustomerId;
                ViewBag.Email = customer.AppUser?.Email ?? customer.Email;
                return View();
            }

            var user = await _userManager.FindByIdAsync(customer.AccountId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản người dùng.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Reset password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                user.IsDefaultPassword = false;
                user.UpdatedAt = DateTime.Now;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = $"Đã reset mật khẩu thành công cho khách hàng {customer.FullName}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.CustomerName = customer.FullName;
            ViewBag.CustomerId = customer.CustomerId;
            ViewBag.Email = customer.AppUser?.Email ?? customer.Email;
            return View();
        }

        private bool CustomerExists(string id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}