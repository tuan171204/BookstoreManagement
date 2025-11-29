using BookstoreManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BookstoreManagement.Services
{
    public class ImportService
    {
        private readonly BookstoreContext _context;

        public ImportService(BookstoreContext context)
        {
            _context = context;
        }

        public async Task CreateImportTicketAsync(ImportTicket ticket)
        {

            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {

                decimal totalCost = 0;
                int totalQty = 0;

                foreach (var detail in ticket.ImportDetails)
                {

                    detail.Subtotal = detail.Quantity * detail.CostPrice * (1 - (detail.Discount ?? 0) / 100);
                    totalCost += detail.Subtotal ?? 0;
                    totalQty += detail.Quantity;
                }


                ticket.TotalCost = totalCost;
                ticket.TotalQuantity = totalQty;
                ticket.Status = "Completed"; 
                ticket.CreatedAt = DateTime.Now;
                ticket.UpdatedAt = DateTime.Now;
    

                _context.ImportTickets.Add(ticket);
                await _context.SaveChangesAsync(); 

                foreach (var detail in ticket.ImportDetails)
                {
                    
                    var book = await _context.Books.FindAsync(detail.BookId);
                    if (book != null)
                    {
                        
                        book.StockQuantity = (book.StockQuantity ?? 0) + detail.Quantity;
                        book.UpdatedAt = DateTime.Now;
                        _context.Books.Update(book);
                    }
                }
                await _context.SaveChangesAsync();


                await transaction.CommitAsync();
            }
            catch (Exception)
            {
   
                await transaction.RollbackAsync();
                throw; 
            }
        }
    }
}