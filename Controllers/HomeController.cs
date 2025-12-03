using BookstoreManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BookstoreManagement.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

		[Authorize]
		public IActionResult Index()
		{
			return View();
		}

		[AllowAnonymous]
		public IActionResult Privacy()
		{
			return View();
		}

		[AllowAnonymous]
		public IActionResult HandleError(int statusCode)
		{
			if (statusCode == 404)
			{
				ViewBag.ErrorMessage = "Xin lỗi, trang bạn tìm không tồn tại.";
				return View("NotFound"); // Trả về view NotFound
			}

			if (statusCode == 403)
			{
				ViewBag.ErrorMessage = "Xin lỗi, bạn không có quyền truy cập tính năng này.";
				return View("AccessDenied"); // Trả về view AccessDenied
			}

			// Xử lý các lỗi khác như 500, v.v.
			return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}


		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
