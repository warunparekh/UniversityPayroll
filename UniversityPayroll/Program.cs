using AspNetCore.Identity.Mongo;
using AspNetCore.Identity.Mongo.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration["MongoDbSettings:ConnectionString"]));

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<LeaveRepository>();
builder.Services.AddScoped<SalaryStructureRepository>();
builder.Services.AddScoped<TaxSlabRepository>();
builder.Services.AddScoped<LeaveBalanceRepository>();
builder.Services.AddScoped<SalarySlipRepository>();
builder.Services.AddScoped<LeaveTypeRepository>();
builder.Services.AddScoped<LeaveEntitlementRepository>();
builder.Services.AddScoped<DesignationRepository>();
builder.Services.AddScoped<NotificationRepository>();

builder.Services
    .AddIdentityMongoDbProvider<ApplicationUser, MongoRole>(identityOptions =>
    {
        identityOptions.SignIn.RequireConfirmedAccount = false;
        identityOptions.Password.RequiredLength = 6;
        identityOptions.Password.RequireDigit = true;
        identityOptions.Password.RequireUppercase = true;
        identityOptions.Password.RequireNonAlphanumeric = true;
    },
    mongoOptions =>
    {
        mongoOptions.ConnectionString = builder.Configuration["MongoDbSettings:ConnectionString"];
    })
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CrudOnlyForAdmin", p => p.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", p => p.RequireRole("User", "Admin"));
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

var app = builder.Build();
await DataSeeder.SeedAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();