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
            var identity = await base.GenerateClaimsAsync(user);

            var roleNames = await UserManager.GetRolesAsync(user);

            // Logic: User -> Roles -> RolePermissions -> Permissions
            if (roleNames.Any())
            {
                var permissions = await (from r in _context.Roles
                                         join rp in _context.RolePermissions on r.Id equals rp.RoleId
                                         join p in _context.Permissions on rp.PermissionId equals p.PermissionId
                                         where roleNames.Contains(r.Name)
                                         select p.PermissionName) // Permission có cột PermissionName (ví dụ: "Book.View", "Book.Edit")
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