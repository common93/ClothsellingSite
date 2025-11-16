using ClothingStore.Core.Entities;
using ClothingStore.Core.Interfaces;
using ClothingStore.Infrastructure.Data;
using ClothingStoreApp.Models;
using ClothingStoreApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
//builder.Services.AddSession();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddHttpContextAccessor();
//builder.Services.AddScoped<CartService>();

builder.Services.AddScoped<ICartService, HybridCartService>();

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

//app.MapGet("/", context =>
//{
//    context.Response.Redirect("/Home/Index");
//    return Task.CompletedTask;
//});

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
            new ClothingStore.Core.Entities.Product { ProductName = "T-Shirt", ProductCategory = "Men", ProductPrice = 499, ProductStockQuantity = 50 },
            new ClothingStore.Core.Entities.Product { ProductName = "Dress", ProductCategory = "Women", ProductPrice = 899, ProductStockQuantity = 30 },
            new ClothingStore.Core.Entities.Product { ProductName = "Jeans",   ProductCategory = "Men", ProductPrice = 1199, ProductStockQuantity = 40 },
            new ClothingStore.Core.Entities.Product { ProductName = "Blouse", ProductCategory = "Women", ProductPrice = 799, ProductStockQuantity = 25 },
            new ClothingStore.Core.Entities.Product { ProductName = "Jacket", ProductCategory = "Unisex", ProductPrice = 1499, ProductStockQuantity = 15 },
            new ClothingStore.Core.Entities.Product { ProductName = "Sweater", ProductCategory = "Women", ProductPrice = 999, ProductStockQuantity = 35 },
            new ClothingStore.Core.Entities.Product { ProductName = "Shorts", ProductCategory = "Men", ProductPrice = 599, ProductStockQuantity = 45 },
            new ClothingStore.Core.Entities.Product { ProductName = "Skirt", ProductCategory = "Women", ProductPrice = 699, ProductStockQuantity = 28 },
            new ClothingStore.Core.Entities.Product { ProductName = "Hoodie", ProductCategory = "Unisex", ProductPrice = 1299, ProductStockQuantity = 20 },
            new ClothingStore.Core.Entities.Product { ProductName = "Socks", ProductCategory = "Unisex", ProductPrice = 199, ProductStockQuantity = 100 },
            new ClothingStore.Core.Entities.Product { ProductName = "Cap", ProductCategory = "Unisex", ProductPrice = 299, ProductStockQuantity = 60 },
            new ClothingStore.Core.Entities.Product { ProductName = "Polo Shirt", ProductCategory = "Men", ProductPrice = 799, ProductStockQuantity = 38 }
        };

        // Optional: Add some random variation to price and stock
        foreach (var p in sampleProducts)
        {
            p.ProductPrice += random.Next(-100, 101); // +/- 100 price variation
            p.ProductStockQuantity += random.Next(-5, 6); // +/- 5 stock variation
        }

        context.Products.AddRange(sampleProducts);
        context.SaveChanges();
    }
}


app.Run();
