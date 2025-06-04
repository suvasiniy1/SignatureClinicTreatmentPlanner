using SignatureClinicTreatmentPlanner.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using SignatureClinicTreatmentPlanner.Models;
using SixLabors.ImageSharp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SignatureClinic_Global_ConnectionString")).EnableSensitiveDataLogging()  // Helps debug issues
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

// Add services to the container
builder.Services.AddControllersWithViews();
// Add Session Services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout (adjust as needed)
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddIdentity<User, IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Login/AccessDenied";
    });
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Preserve C# property names
    });

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
// Enable Session Middleware
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ✅ **Directly Map Controllers Instead of UseEndpoints()**
app.MapControllerRoute(
    name: "default",
   // pattern: "{controller=Patient}/{action=Create}/{id?}");
    pattern: "{controller=Login}/{action=Index}/{id?}");

//app.MapRazorPages();

app.Run();
