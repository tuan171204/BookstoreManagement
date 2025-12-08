// Thư mục: Services/SupplierService.cs

using BookstoreManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookstoreManagement.Services
{
    public class SupplierService
    {
        private readonly BookstoreContext _context;

        public SupplierService(BookstoreContext context)
        {
            _context = context;
        }

        public async Task<List<Supplier>> GetAllSuppliersAsync()
        {
            return await _context.Suppliers
                                 .Where(s => s.IsActive == true)
                                 .OrderBy(s => s.Name)
                                 .ToListAsync();
        }

        // GetByID
        public async Task<Supplier> GetSupplierByIdAsync(int id)
        {
            // Vẫn cho phép lấy supplier đã bị "ẩn" để xem/chỉnh sửa
            return await _context.Suppliers.FindAsync(id);
        }

        // Thêm mới
        public async Task AddSupplierAsync(Supplier supplier)
        {
            supplier.CreatedAt = DateTime.Now;
            supplier.UpdatedAt = DateTime.Now;
            supplier.IsActive = true;

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
        }

        // Cập nhật
        public async Task UpdateSupplierAsync(Supplier supplier)
        {
            var existingSupplier = await _context.Suppliers.FindAsync(supplier.SupplierId);
            if (existingSupplier != null)
            {
                existingSupplier.Name = supplier.Name;
                existingSupplier.ContactInfo = supplier.ContactInfo;
                existingSupplier.Address = supplier.Address;
                existingSupplier.IsActive = supplier.IsActive;
                existingSupplier.UpdatedAt = DateTime.Now;

                _context.Suppliers.Update(existingSupplier);
                await _context.SaveChangesAsync();
            }
        }

        // Xóa (SỬA LẠI THÀNH "SOFT DELETE" - CHỈ ẨN ĐI)
        public async Task DeleteSupplierAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                supplier.IsActive = false;
                supplier.UpdatedAt = DateTime.Now;
                _context.Suppliers.Update(supplier);
                await _context.SaveChangesAsync();
            }
        }
    }
}