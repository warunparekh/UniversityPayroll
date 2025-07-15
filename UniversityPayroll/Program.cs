using MongoDB.Driver;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using AspNetCore.Identity.Mongo;         
using AspNetCore.Identity.Mongo.Model;  
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;

namespace UniversityPayroll
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

            builder.Services.AddSingleton<IMongoClient, MongoClient>(
                sp => new MongoClient(builder.Configuration.GetSection("MongoDbSettings:ConnectionString").Value)
            );

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSingleton<MongoDbContext>();

            builder.Services.AddScoped<EmployeeRepository>();
            builder.Services.AddScoped<LeaveRepository>();



            builder.Services.AddIdentityMongoDbProvider<ApplicationUser, MongoRole>(opts =>
                {
                    opts.SignIn.RequireConfirmedAccount = false;
                    opts.Password.RequiredLength = 6;
                    opts.Password.RequireDigit = true;
                    opts.Password.RequireUppercase = true;
                    opts.Password.RequireNonAlphanumeric = true;

                },
                mongoOpts =>
                {
                    mongoOpts.ConnectionString = builder.Configuration["MongoDbSettings:ConnectionString"];
                    
                })
                .AddDefaultTokenProviders();

            builder.Services.AddRazorPages();
            

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("CrudOnlyForAdmin", p => p.RequireRole("Admin"));
                options.AddPolicy("UserOrAdmin", p => p.RequireRole("User", "Admin"));
            });




            var app = builder.Build();

            // Configure the HTTP request pipeline.
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
        }
    }
}
