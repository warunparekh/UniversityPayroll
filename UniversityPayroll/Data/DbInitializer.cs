using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> um,
            RoleManager<ApplicationRole> rm)
        {
            if (!await rm.RoleExistsAsync("Admin"))
                await rm.CreateAsync(new ApplicationRole { Name = "Admin" });
            if (!await rm.RoleExistsAsync("Employee"))
                await rm.CreateAsync(new ApplicationRole { Name = "Employee" });

            var admin = await um.FindByNameAsync("admin@uni.edu");
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin@uni.edu",
                    Email = "admin@uni.edu",
                    FullName = "System Administrator"
                };
                await um.CreateAsync(admin, "Admin#123");
                await um.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
