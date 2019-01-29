using Microsoft.AspNetCore.Identity;
using MyCodeCamp.Data.Entities;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyCodeCamp.Data
{
    public class CampIdentityInitializer
    {
        private RoleManager<IdentityRole> _roleMgr;
        private UserManager<CampUser> _userMgr;

        public CampIdentityInitializer(UserManager<CampUser> userMgr, RoleManager<IdentityRole> roleMgr)
        {
            _userMgr = userMgr;
            _roleMgr = roleMgr;
        }

        public async Task Seed()
        {
            var user = await _userMgr.FindByNameAsync("vioflu");

            // Add User
            if (user == null)
            {
                if (!(await _roleMgr.RoleExistsAsync("Admin")))
                {
                    var adminRole = new IdentityRole("Admin");
                    await _roleMgr.CreateAsync(adminRole);
                    await _roleMgr.AddClaimAsync(adminRole, new Claim("IsAdmin", "True"));
                    //var role = new IdentityRole("Admin");
                    //role.Claims.Add(new IdentityRoleClaim<string>() { ClaimType = "IsAdmin", ClaimValue = "True" });
                    //await _roleMgr.CreateAsync(role);
                }

                user = new CampUser()
                {
                    UserName = "vioflu",
                    FirstName = "vioflu",
                    LastName = "vioflu",
                    Email = "vf@gmail.com"
                };

                var userResult = await _userMgr.CreateAsync(user, "P@ssw0rd");
                var roleResult = await _userMgr.AddToRoleAsync(user, "Admin");
                var claimResult = await _userMgr.AddClaimAsync(user, new Claim("SuperUser", "True"));

                if (!userResult.Succeeded || !roleResult.Succeeded || !claimResult.Succeeded)
                {
                    throw new InvalidOperationException("Failed to build user and roles");
                }

            }
        }
    }
}
