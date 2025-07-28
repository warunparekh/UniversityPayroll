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

// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

var connectionString = builder.Configuration["MongoDbSettings:ConnectionString"];
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
builder.Services.AddSingleton<MongoDbContext>();

// Register Repositories
var repositoryTypes = new[]
{
    typeof(EmployeeRepository),
    typeof(LeaveRepository),
    typeof(SalaryStructureRepository),
    typeof(TaxSlabRepository),
    typeof(LeaveBalanceRepository),
    typeof(SalarySlipRepository),
    typeof(LeaveTypeRepository),
    typeof(LeaveEntitlementRepository),
    typeof(DesignationRepository),
    typeof(NotificationRepository)
};

foreach (var repoType in repositoryTypes)
{
    builder.Services.AddScoped(repoType);
}

// Configure Identity
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
        mongoOptions.ConnectionString = connectionString;
    })
    .AddDefaultTokenProviders();

// Configure MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CrudOnlyForAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

var app = builder.Build();

// Seed initial data
await DataSeeder.SeedAsync(app);

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
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