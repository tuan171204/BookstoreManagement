using BookstoreManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookstoreManagement.ViewModels.Sales
{
    public class CreateOrderViewModel
    {
        public required Order Order { get; set; }
        public SelectList? Customers { get; set; }
        public SelectList? Books { get; set; }
        public SelectList? PaymentMethods { get; set; }
    }
}