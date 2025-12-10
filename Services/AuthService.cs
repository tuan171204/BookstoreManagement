using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BookstoreManagement.Models;

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

            // Lấy danh sách RoleId của user
            var userRoleIds = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (!userRoleIds.Any())
            {
                return new List<string>();
            }

            // Join bảng Permissions lấy PermissionName 
            var permissions = await _context.RolePermissions
                .Where(rp => userRoleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.PermissionName)
                .Distinct()
                .ToListAsync();

            return permissions;
        }
    }

}
