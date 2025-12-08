using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Warehouse
{ 

       public class ImportDetailItem
        {
            [Required]
            public int BookId { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
            public int Quantity { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Giá nhập phải là số dương")]
            public decimal CostPrice { get; set; }
        }

        // Đây là ViewModel chính cho View "Create"
        public class ImportTicketCreateViewModel
        {

            
            // === Phần 1: Thông tin Master (ImportTicket) ===
            [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
            public int SupplierId { get; set; }

            public int? PaymentMethodId { get; set; }

            [MaxLength(255)]
            public string? Note { get; set; }

            // === Phần 2: Danh sách Details (ImportDetail) ===
            // JavaScript sẽ tạo ra danh sách này để gửi về server
            public List<ImportDetailItem> Details { get; set; } = new List<ImportDetailItem>();

            // === Phần 3: Dữ liệu cho các Dropdown ===
            // Controller sẽ đổ dữ liệu vào đây
            public SelectList? Suppliers { get; set; }
            public SelectList? PaymentMethods { get; set; }
            public SelectList? Books { get; set; } // Dùng cho template thêm dòng
        }
    }
