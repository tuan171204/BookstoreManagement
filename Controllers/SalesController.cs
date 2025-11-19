using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookstoreManagement.Controllers
{
    public class SalesController : Controller
    {
        [HttpGet]
        [Authorize]
        public IActionResult List()
        {
            return View();
        }
    }
}