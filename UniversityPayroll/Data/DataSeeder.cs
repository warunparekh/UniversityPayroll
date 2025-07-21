using AspNetCore.Identity.Mongo.Model;
using Microsoft.AspNetCore.Identity;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<MongoRole>>();
                var leaveTypeRepo = scope.ServiceProvider.GetRequiredService<LeaveTypeRepository>();
                var structureRepo = scope.ServiceProvider.GetRequiredService<SalaryStructureRepository>();
                var taxSlabRepo = scope.ServiceProvider.GetRequiredService<TaxSlabRepository>();
                var entitlementRepo = scope.ServiceProvider.GetRequiredService<LeaveEntitlementRepository>();

                // --- Seed Roles: Admin and User ---
                foreach (var roleName in new[] { "Admin", "User" })
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new MongoRole(roleName));
                    }
                }

                // --- Seed Admin User ---
                const string adminEmail = "hr@uni.edu";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                    await userManager.CreateAsync(adminUser, "Password1@");
                    await userManager.AddToRolesAsync(adminUser, new[] { "Admin", "User" });
                }

                // --- Seed Foundational Data (without creating employees) ---
                if (!(await leaveTypeRepo.GetAllAsync()).Any())
                {
                    await leaveTypeRepo.CreateAsync(new LeaveType { Name = "Sick" });
                    await leaveTypeRepo.CreateAsync(new LeaveType { Name = "Casual" });
                    await leaveTypeRepo.CreateAsync(new LeaveType { Name = "Unpaid" });
                }

                if (!(await structureRepo.GetAllAsync()).Any())
                {
                    await structureRepo.CreateAsync(new SalaryStructure
                    {
                        Designation = "Professor",
                        Allowances = new Allowances { DaPercent = 10, HraPercent = 20 },
                        Pf = new PfRules { EmployeePercent = 12, EmployerPercent = 12, EdliPercent = 0.5 },
                        AnnualIncrementPercent = 5
                    });
                }

                if (!(await taxSlabRepo.GetAllAsync()).Any())
                {
                    await taxSlabRepo.CreateAsync(new TaxSlab
                    {
                        FinancialYear = "2024-2025",
                        CessPercent = 4,
                        Slabs = new List<Slab>
                        {
                            new Slab { From = 0, To = 300000, Rate = 0 },
                            new Slab { From = 300001, To = 600000, Rate = 5 },
                            new Slab { From = 600001, To = 900000, Rate = 10 }
                        }
                    });
                }

                if (!(await entitlementRepo.GetAllAsync()).Any())
                {
                    await entitlementRepo.CreateAsync(new LeaveEntitlement
                    {
                        Designation = "Professor",
                        Entitlements = new Dictionary<string, int> { { "Sick", 12 }, { "Casual", 6 } }
                    });
                }
            }
        }
    }
}