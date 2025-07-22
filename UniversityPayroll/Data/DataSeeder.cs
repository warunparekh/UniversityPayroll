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
                var designationRepo = scope.ServiceProvider.GetRequiredService<DesignationRepository>();

                foreach (var roleName in new[] { "Admin", "User" })
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new MongoRole(roleName));
                    }
                }

                const string adminEmail = "hr@uni.edu";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                    await userManager.CreateAsync(adminUser, "Password1@");
                    await userManager.AddToRolesAsync(adminUser, new[] { "Admin", "User" });
                }

                if (!(await leaveTypeRepo.GetAllAsync()).Any())
                {
                    await leaveTypeRepo.CreateAsync(new LeaveType { Name = "Sick" });
                    await leaveTypeRepo.CreateAsync(new LeaveType { Name = "Casual" });
                    await leaveTypeRepo.CreateAsync(new LeaveType { Name = "Unpaid" });
                }

                var existingDesignations = await designationRepo.GetAllAsync();
                if (!existingDesignations.Any())
                {
                    var defaultDesignations = new[]
                    {
                        new Designation { Name = "Professor", Description = "Senior academic position with research responsibilities", IsActive = true },
                        new Designation { Name = "Associate Professor", Description = "Mid-level academic position", IsActive = true },
                        new Designation { Name = "Assistant Professor", Description = "Entry-level academic position", IsActive = true },
                        new Designation { Name = "Clerk", Description = "Administrative support staff", IsActive = true },
                        new Designation { Name = "Accountant", Description = "Financial management and accounting staff", IsActive = true },
                        new Designation { Name = "Lab Assistant", Description = "Laboratory support staff", IsActive = true },
                        new Designation { Name = "Librarian", Description = "Library management staff", IsActive = true }
                    };

                    foreach (var designation in defaultDesignations)
                    {
                        await designationRepo.CreateAsync(designation);
                    }
                }

                var activeDesignations = await designationRepo.GetActiveAsync();
                var existingStructures = await structureRepo.GetAllAsync();
                var existingStructureDesignations = existingStructures.Select(s => s.Designation).ToHashSet();

                foreach (var designation in activeDesignations)
                {
                    if (!existingStructureDesignations.Contains(designation.Name))
                    {
                        var allowances = new Allowances();
                        var incrementPercent = 3;

                        if (designation.Name.Contains("Professor"))
                        {
                            allowances.DaPercent = 12;
                            allowances.HraPercent = 20;
                            incrementPercent = 5;
                        }
                        else if (designation.Name == "Librarian")
                        {
                            allowances.DaPercent = 10;
                            allowances.HraPercent = 18;
                            incrementPercent = 4;
                        }
                        else
                        {
                            allowances.DaPercent = 8;
                            allowances.HraPercent = 15;
                            incrementPercent = 3;
                        }

                        await structureRepo.CreateAsync(new SalaryStructure
                        {
                            Designation = designation.Name,
                            Allowances = allowances,
                            Pf = new PfRules
                            {
                                EmployeePercent = 12,
                                EmployerPercent = 12,
                                EdliPercent = 0.5,
                                PfWageCeiling = 15000
                            },
                            AnnualIncrementPercent = incrementPercent,
                            CreatedOn = DateTime.UtcNow,
                            UpdatedOn = DateTime.UtcNow
                        });
                    }
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
                            new Slab { From = 600001, To = 900000, Rate = 10 },
                            new Slab { From = 900001, To = 1200000, Rate = 15 },
                            new Slab { From = 1200001, To = 1500000, Rate = 20 },
                            new Slab { From = 1500001, To = int.MaxValue, Rate = 30 }
                        }
                    });

                    await taxSlabRepo.CreateAsync(new TaxSlab
                    {
                        FinancialYear = "2025-2026",
                        CessPercent = 4,
                        Slabs = new List<Slab>
                        {
                            new Slab { From = 0, To = 300000, Rate = 0 },
                            new Slab { From = 300001, To = 700000, Rate = 5 },
                            new Slab { From = 700001, To = 1000000, Rate = 10 },
                            new Slab { From = 1000001, To = 1200000, Rate = 15 },
                            new Slab { From = 1200001, To = 1500000, Rate = 20 },
                            new Slab { From = 1500001, To = int.MaxValue, Rate = 30 }
                        }
                    });
                }

                var existingEntitlements = await entitlementRepo.GetAllAsync();
                var existingEntitlementDesignations = existingEntitlements.Select(e => e.Designation).ToHashSet();

                foreach (var designation in activeDesignations)
                {
                    if (!existingEntitlementDesignations.Contains(designation.Name))
                    {
                        var entitlements = new Dictionary<string, int>();

                        if (designation.Name.Contains("Professor"))
                        {
                            entitlements = new Dictionary<string, int>
                            {
                                { "Sick", 15 },
                                { "Casual", 8 }
                            };
                        }
                        else if (designation.Name == "Librarian")
                        {
                            entitlements = new Dictionary<string, int>
                            {
                                { "Sick", 12 },
                                { "Casual", 6 }
                            };
                        }
                        else
                        {
                            entitlements = new Dictionary<string, int>
                            {
                                { "Sick", 10 },
                                { "Casual", 5 }
                            };
                        }

                        await entitlementRepo.CreateAsync(new LeaveEntitlement
                        {
                            Designation = designation.Name,
                            Entitlements = entitlements
                        });
                    }
                }
            }
        }
    }
}