using BookstoreManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BookstoreManagement.Services
{
    public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, AppRole>
    {
        private readonly BookstoreContext _context;

        public CustomClaimsPrincipalFactory(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor,
            BookstoreContext context)
            : base(userManager, roleManager, optionsAccessor)
        {
            _context = context;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
        {
            // 1. Lấy identity mặc định (bao gồm User info và Role Names)
            var identity = await base.GenerateClaimsAsync(user);

            // 2. Lấy danh sách Role của user
            var roleNames = await UserManager.GetRolesAsync(user);

            // 3. Tìm các Permission tương ứng với Role đó trong DB của bạn
            // Logic: User -> Roles -> RolePermissions -> Permissions
            if (roleNames.Any())
            {
                var permissions = await (from r in _context.Roles
                                         join rp in _context.RolePermissions on r.Id equals rp.RoleId
                                         join p in _context.Permissions on rp.PermissionId equals p.PermissionId
                                         where roleNames.Contains(r.Name)
                                         select p.PermissionName) // Giả sử Permission có cột PermissionName (ví dụ: "Book.View", "Book.Edit")
                                         .Distinct()
                                         .ToListAsync();

                // 4. Add Permission vào Claims
                foreach (var permission in permissions)
                {
                    identity.AddClaim(new Claim("Permission", permission));
                }
            }

            return identity;
        }
    }
}