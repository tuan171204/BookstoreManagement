using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookstoreManagement.Controllers
{
    public class BookController : Controller
    {
        [HttpGet]
        [Authorize]

        public IActionResult Index()
        {
            return View();
        }
    }
}