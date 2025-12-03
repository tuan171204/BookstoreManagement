// Đặt trong Service xử lý Login (Ví dụ: AuthService.cs)
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BookstoreManagement.Models; // Namespace chứa DbContext và Models

namespace BookstoreManagement.Services
{
    public class AuthService
    {
        private readonly BookstoreContext _context;

        public AuthService(BookstoreContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetUserPermissionsAsync(string userId)
        {
            // Giả sử: Bạn có bảng UserRole để kết nối AppUser với AppRole
            // Nếu bạn dùng Identity, mối quan hệ sẽ là IdentityUserRole<TKey>

            // 1. Lấy danh sách RoleId của người dùng (Giả định: RoleId là string)
            var userRoleIds = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (!userRoleIds.Any())
            {
                return new List<string>();
            }

            // 2. Truy vấn đến bảng RolePermission để lấy tất cả PermissionId liên quan
            // 3. Join với bảng Permissions để lấy PermissionName ("Order.View")
            var permissions = await _context.RolePermissions
                .Where(rp => userRoleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.PermissionName) // Ánh xạ qua Navigation Property
                .Distinct()
                .ToListAsync();

            return permissions;
        }
    }

}
