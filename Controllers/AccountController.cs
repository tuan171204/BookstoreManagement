using BookstoreManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using BookstoreManagement.ViewModels.Account;

namespace BookstoreManagement.Controllers
{
    public class AccountController : Controller
    {
        // Xử lý đăng nhập của Identity
        private readonly SignInManager<AppUser> _signInManager;

        // Quản lý người dùng của Identity
        private readonly UserManager<AppUser> _userManager;

        private readonly IEmailSender _emailSender;

        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // ------------------ LOGIN & LOGOUT ------------------
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
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            if (!ModelState.IsValid) // Các validation trong models không hợp lệ
            {
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                userName: email,
                password: password,
                isPersistent: rememberMe, // true = lưu cookie sau khi tắt trình duyệt
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user != null && user.IsDefaultPassword)
                {
                    TempData["WarningMessage"] = "Bạn đang sử dụng mật khẩu mặc định. Vui lòng đổi mật khẩu ngay để bảo mật tài khoản!";
                    return RedirectToAction("Index", "Setting");
                }

                return RedirectToAction("Index", "Shopping");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Tài khoản bị khóa tạm thời");
            }
            else
            {
                ModelState.AddModelError("", "Đăng nhập thất bại. Vui lòng kiểm tra lại");
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


        // ------------------ FORGOT PASSWORD ------------------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model); // Hiển thị lỗi validation
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }


            // DEBUG LOG
            Console.WriteLine($"Email user: {user.Email}");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { token, email = user.Email },
                                                                        protocol: Request.Scheme,
                                                                        host: Request.Host.ToString());

            await _emailSender.SendEmailAsync(user.Email, "Đặt lại mật khẩu",
            $"<p>Nhấn vào <a href='{resetLink}'>đây</a> để đặt lại mật khẩu.</p>");

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();


        // ------------------ RESET PASSWORD ------------------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                user.IsDefaultPassword = false;
                user.UpdatedAt = DateTime.Now;

                await _userManager.UpdateAsync(user);

                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);

        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action("ResetPassword", "Account",
                new { token = token, email = user.Email },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Yêu cầu đổi mật khẩu",
                $"<p>Bạn đã yêu cầu đổi mật khẩu. Nhấn vào <a href='{resetLink}'>đây</a> để thiết lập mật khẩu mới.</p>");

            return View("ForgotPasswordConfirmation");
        }
    }
}