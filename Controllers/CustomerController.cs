using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return Json(new { success = false, message = "Không tìm khách hàng!" });

            try
            {
                customer.IsActive = false;
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa khách hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Xóa thất bại: " + ex.Message });
            }
        }

        public async Task<IActionResult> GetUpdateForm(string id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            var vm = new CustomerViewModel
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                IsActive = customer.IsActive,
            };

            return PartialView("~/Views/Customer/_UpdateForm.cshtml", vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_UpdateForm", model);
            }

            var customer = await _context.Customers.FindAsync(model.CustomerId);
            if (customer == null) return NotFound();

            customer.FullName = model.FullName;
            customer.Email = model.Email;
            customer.Phone = model.Phone;
            customer.Address = model.Address;
            customer.IsActive = model.IsActive;
            customer.UpdatedAt = DateTime.Now;

            try
            {
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật khách hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Cập nhật thất bại: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetList(string inputTxt)
        {

            TempData["CurrentFeature"] = "Customer";

            var query = from u in _context.Customers
                        select u;
            if (!string.IsNullOrEmpty(inputTxt))
            {
                query = query.Where(u => u.FullName.Contains(inputTxt)
                                    || u.Phone.Contains(inputTxt)
                                    || (u.Email.Contains("@") && u.Email.Substring(0, u.Email.IndexOf("@")).Contains(inputTxt))
                                    || u.Address.Contains(inputTxt)
                );

            }

            var customers = await query.Distinct().ToListAsync();
            return View("Index", customers);
        }

    }
}