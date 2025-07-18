using Microsoft.Extensions.Options;
using AspNetCore.Identity.Mongo;
using AspNetCore.Identity.Mongo.Model;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using UniversityPayroll.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<DatabaseSettings>>().Value);

var conn = builder.Configuration["DatabaseSettings:ConnectionString"];
var db = builder.Configuration["DatabaseSettings:DatabaseName"];
builder.Services.AddIdentityMongoDbProvider<ApplicationUser, ApplicationRole>(
    idOpts => { idOpts.Password.RequiredLength = 6; },
    mongoOpts => { mongoOpts.ConnectionString = $"{conn}/{db}"; })
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<DesignationService>();
builder.Services.AddSingleton<SalaryStructureService>();
builder.Services.AddSingleton<TaxSlabService>();
builder.Services.AddSingleton<EmployeeService>();
builder.Services.AddSingleton<LeaveRequestService>();
builder.Services.AddSingleton<LeaveApprovalService>();
builder.Services.AddSingleton<SalarySlipService>();
builder.Services.AddSingleton<DesignationService>();
builder.Services.AddSingleton<LeaveTypeService>();
builder.Services.AddSingleton<LeaveEntitlementService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

using var scope = app.Services.CreateScope();
await DbInitializer.SeedAsync(
    scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
    scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
