using Microsoft.AspNetCore.Mvc;

public class PartialController : Controller
{
    [HttpGet]
    public IActionResult GetAddForm(string feature)
    {
        switch (feature)
        {
            // Trả về View/Features tương ứng/_CreateForm.cshtml và đặt vào body của _ModalForm.cshtml
            case "Customer":
                return PartialView("~/Views/Customer/_CreateForm.cshtml");
            case "Employee":
                return PartialView("~/Views/Employee/_CreateForm.cshtml");
            case "Book":
                return PartialView("~/Views/Book/_CreateForm.cshtml");
            case "Sales":
                return PartialView("~/Views/Sales/_CreateForm.cshtml");
            default:
                return Content("<p>Không có biểu mẫu cho chức năng này.</p>");
        }
    }
}