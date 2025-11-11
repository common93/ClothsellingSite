using ClothingStore.Core.Entities;
using ClothingStore.Core.Interfaces;
using ClothingStore.Infrastructure.Data;
using ClothingStoreApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();

// Database connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IProductRepository, ProductRepository>();



builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Admin", "Customer" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAuthentication();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();



app.UseAuthorization();
app.UseSession(); //use before map


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!context.Products.Any())
    {
        var random = new Random();

        var sampleProducts = new List<ClothingStore.Core.Entities.Product>
        {
            new ClothingStore.Core.Entities.Product { Name = "T-Shirt", Category = "Men", Price = 499, StockQuantity = 50 },
            new ClothingStore.Core.Entities.Product { Name = "Dress", Category = "Women", Price = 899, StockQuantity = 30 },
            new ClothingStore.Core.Entities.Product { Name = "Jeans", Category = "Men", Price = 1199, StockQuantity = 40 },
            new ClothingStore.Core.Entities.Product { Name = "Blouse", Category = "Women", Price = 799, StockQuantity = 25 },
            new ClothingStore.Core.Entities.Product { Name = "Jacket", Category = "Unisex", Price = 1499, StockQuantity = 15 },
            new ClothingStore.Core.Entities.Product { Name = "Sweater", Category = "Women", Price = 999, StockQuantity = 35 },
            new ClothingStore.Core.Entities.Product { Name = "Shorts", Category = "Men", Price = 599, StockQuantity = 45 },
            new ClothingStore.Core.Entities.Product { Name = "Skirt", Category = "Women", Price = 699, StockQuantity = 28 },
            new ClothingStore.Core.Entities.Product { Name = "Hoodie", Category = "Unisex", Price = 1299, StockQuantity = 20 },
            new ClothingStore.Core.Entities.Product { Name = "Socks", Category = "Unisex", Price = 199, StockQuantity = 100 },
            new ClothingStore.Core.Entities.Product { Name = "Cap", Category = "Unisex", Price = 299, StockQuantity = 60 },
            new ClothingStore.Core.Entities.Product { Name = "Polo Shirt", Category = "Men", Price = 799, StockQuantity = 38 }
        };

        // Optional: Add some random variation to price and stock
        foreach (var p in sampleProducts)
        {
            p.Price += random.Next(-100, 101); // +/- 100 price variation
            p.StockQuantity += random.Next(-5, 6); // +/- 5 stock variation
        }

        context.Products.AddRange(sampleProducts);
        context.SaveChanges();
    }
}


app.Run();
