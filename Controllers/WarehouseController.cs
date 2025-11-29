using BookstoreManagement.Models;
using BookstoreManagement.Services;
using BookstoreManagement.ViewModels.Warehouse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class WarehouseController : Controller
{
    private readonly BookstoreContext _context;
    private readonly ImportService _importService;


    public WarehouseController(BookstoreContext context, ImportService importService)
    {
        _context = context;
        _importService = importService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string searchString, string typeFilter, string statusFilter)
    {

        ViewBag.TypeFilter = new SelectList(new[] { "Import", "Export" });
        ViewBag.StatusFilter = new SelectList(new[] { "Completed", "Pending", "Cancelled" });
        ViewData["CurrentFilter"] = searchString;


        var importQuery = _context.ImportTickets
            .Include(t => t.Supplier)
            .Select(t => new WarehouseTicketViewModel
            {
                Id = t.ImportId,
                Type = "Import",
                DocumentNumber = t.DocumentNumber,
                Date = t.Date,
                Reference = t.Supplier.Name,
                TotalQuantity = t.TotalQuantity,
                TotalCost = t.TotalCost,
                Status = t.Status
            });


        var exportQuery = _context.ExportTickets
            .Include(t => t.Reference)
            .Select(t => new WarehouseTicketViewModel
            {
                Id = t.ExportId,
                Type = "Export",
                DocumentNumber = t.DocumentNumber,
                Date = t.Date,

                Reference = t.Reason == "Sale" && t.Reference != null ? $"ĐH: {t.Reference.OrderId}" : t.Reason,
                TotalQuantity = t.TotalQuantity,
                TotalCost = null,
                Status = t.Status
            });


        if (!String.IsNullOrEmpty(searchString))
        {
            importQuery = importQuery.Where(t => (t.DocumentNumber != null && t.DocumentNumber.Contains(searchString))
                                              || t.Reference.Contains(searchString));
            exportQuery = exportQuery.Where(t => (t.DocumentNumber != null && t.DocumentNumber.Contains(searchString))
                                              || t.Reference.Contains(searchString));
        }

        if (!String.IsNullOrEmpty(statusFilter))
        {
            importQuery = importQuery.Where(t => t.Status == statusFilter);
            exportQuery = exportQuery.Where(t => t.Status == statusFilter);
        }

        var importList = new List<WarehouseTicketViewModel>();
        var exportList = new List<WarehouseTicketViewModel>();


        if (string.IsNullOrEmpty(typeFilter) || typeFilter == "Import")
        {
            importList = await importQuery.ToListAsync();
        }

        if (string.IsNullOrEmpty(typeFilter) || typeFilter == "Export")
        {
            exportList = await exportQuery.ToListAsync();
        }

        var allTickets = importList.Concat(exportList);


        var finalTickets = allTickets
            .OrderByDescending(t => t.Date)
            .ToList();

        return View(finalTickets);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string type)
    {
        var viewModel = new WarehouseDetailViewModel { TicketType = type };

        if (type == "Import")
        {
            viewModel.ImportTicket = await _context.ImportTickets
                .Include(t => t.Supplier)
                .Include(t => t.User)
                .Include(t => t.ImportDetails)
                    .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(t => t.ImportId == id);
        }
        else 
        {
            viewModel.ExportTicket = await _context.ExportTickets
                .Include(t => t.Reference)
                .Include(t => t.User)
                .Include(t => t.ExportDetails)
                    .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(t => t.ExportId == id);
        }

        if (viewModel.ImportTicket == null && viewModel.ExportTicket == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var viewModel = new ImportTicketCreateViewModel
        {
            Suppliers = new SelectList(_context.Suppliers.ToList(), "SupplierId", "Name"),
            PaymentMethods = new SelectList(_context.Codes.Where(c => c.Entity == "PaymentMethod").ToList(), "CodeId", "Value"),
            Books = new SelectList(_context.Books.Where(b => b.IsDeleted == false).ToList(), "BookId", "Title")
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ImportTicketCreateViewModel viewModel)
    {

        if (viewModel.Details == null || !viewModel.Details.Any())
        {
            ModelState.AddModelError("Details", "Bạn phải thêm ít nhất một quyển sách.");
        }

        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var importTicket = new ImportTicket
            {
                UserId = userId,
                SupplierId = viewModel.SupplierId,
                PaymentMethodId = viewModel.PaymentMethodId,
                Note = viewModel.Note,
                Date = DateTime.Now,
                Status = "Completed",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                DocumentNumber = $"PN{DateTimeOffset.Now.ToUnixTimeSeconds()}"
            };


            foreach (var item in viewModel.Details)
            {
                importTicket.ImportDetails.Add(new ImportDetail
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    CostPrice = item.CostPrice,
                    Subtotal = item.Quantity * item.CostPrice
                });
            }

            try
            {
            
         
                await _importService.CreateImportTicketAsync(importTicket);

                TempData["SuccessMessage"] = "Tạo phiếu nhập thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
         
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu phiếu nhập. Vui lòng thử lại.");
                await LoadDropdownsForCreateView(viewModel);
                return View(viewModel);
            }
        }


        await LoadDropdownsForCreateView(viewModel);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetBooksBySupplier(int supplierId)
    {

        var supplierBooks = await _context.SupplierBooks
            .Where(sb => sb.SupplierId == supplierId)
            .Include(sb => sb.Book) 
            .Select(sb => new
            {
                BookId = sb.BookId,
                Title = sb.Book.Title,
                
                CostPrice = sb.DefaultCostPrice
            })
            .ToListAsync();

       
        return Json(supplierBooks);
    }

    
    private async Task LoadDropdownsForCreateView(ImportTicketCreateViewModel viewModel)
    {
        viewModel.Suppliers = new SelectList(await _context.Suppliers.ToListAsync(), "SupplierId", "Name", viewModel.SupplierId);
        viewModel.PaymentMethods = new SelectList(await _context.Codes.Where(c => c.Entity == "PaymentMethod").ToListAsync(), "CodeId", "Value", viewModel.PaymentMethodId);
        viewModel.Books = new SelectList(await _context.Books.Where(b => b.IsDeleted == false).ToListAsync(), "BookId", "Title");
    }

    
    [HttpGet]
    public IActionResult CreateExport()
    {
        var viewModel = new ExportTicketCreateViewModel
        {
          
            Books = new SelectList(_context.Books.Where(b => b.IsDeleted != true && b.StockQuantity > 0).ToList(), "BookId", "Title")
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExport(ExportTicketCreateViewModel viewModel)
    {
        if (viewModel.Details == null || !viewModel.Details.Any())
        {
            ModelState.AddModelError("Details", "Bạn phải chọn ít nhất một quyển sách.");
        }

        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
              
                var exportTicket = new ExportTicket
                {
                    UserId = userId ?? "1",
                    Date = DateTime.Now,
                    Status = "Completed",
                    Reason = viewModel.Reason,
                    Note = viewModel.Note,
                    DocumentNumber = $"PX{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };


                int totalQty = 0;

                foreach (var item in viewModel.Details)
                {
                    var book = await _context.Books.FindAsync(item.BookId);

                 
                    if (book == null)
                    {
                        throw new Exception($"Sách ID {item.BookId} không tồn tại.");
                    }

                   
                    if ((book.StockQuantity ?? 0) < item.Quantity)
                    {
                        throw new Exception($"Sách '{book.Title}' không đủ số lượng để xuất (Tồn: {book.StockQuantity}).");
                    }

                   
                    book.StockQuantity -= item.Quantity;
                    book.UpdatedAt = DateTime.Now;

                   
                    exportTicket.ExportDetails.Add(new ExportDetail
                    {
                        BookId = item.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = book.Price, 
                        Subtotal = book.Price * item.Quantity
                    });

                    totalQty += item.Quantity;
                }

                exportTicket.TotalQuantity = totalQty;


                _context.ExportTickets.Add(exportTicket);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Tạo phiếu xuất kho thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
            }
        }


        viewModel.Books = new SelectList(await _context.Books.Where(b => b.IsDeleted != true).ToListAsync(), "BookId", "Title");
        return View(viewModel);
    }
}