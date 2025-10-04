using BookstoreManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BookstoreManagement.Controllers
{
    public class AccountController : Controller
    {
        // Xử lý đăng nhập của Identity
        private readonly SignInManager<AppUser> _signInManager;

        // Quản lý người dùng của Identity
        private readonly UserManager<AppUser> _userManager;

        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // Hiển thị trang đăng nhập
        [HttpGet]
        [AllowAnonymous] // Cho phép người chưa đăng nhập truy cập
        public IActionResult Login()
        {
            return View();
        }

        // Submit form đăng nhập
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken] // Bảo vệ tấn công CSRF
        public async Task<IActionResult> Login(string email, string password)
        {
            if (!ModelState.IsValid) // Các validation trong models không hợp lệ
            {
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                userName: email,
                password: password,
                isPersistent: false, // true = lưu cookie sau khi tắt trình duyệt
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Tài khoản bị khóa tạm thời");
            }
            else
            {
                ModelState.AddModelError("", "Đăng nhập thất . Vui lòng kiểm tra lại");
            }

            return View();

        }

        // Đăng xuất
        [HttpPost]
        [Authorize] // Chỉ cho phép người đăng nhập thực hiện
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}