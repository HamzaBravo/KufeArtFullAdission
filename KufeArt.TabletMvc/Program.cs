using AppDbContext;
using KufeArtFullAdission.Mvc.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 🎯 DATABASE
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HamzaLocal")));

// 🔐 AUTHENTICATION
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.Cookie.Name = "TabletAuth";
        options.ExpireTimeSpan = TimeSpan.FromHours(12); // Tablet için daha uzun session
    });

// 🌐 SIGNALR
builder.Services.AddSignalR();

// 📞 HTTP CLIENT (Admin paneli ile iletişim)
builder.Services.AddHttpClient("AdminPanel", client =>
{
    var adminUrl = builder.Environment.IsDevelopment()
        ? "https://localhost:7164"
        : "https://adisyon.kufeart.com";
    client.BaseAddress = new Uri(adminUrl);
});

// MVC Services
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
    pattern: "{controller=Home}/{action=Login}/{id?}");


app.MapHub<OrderHub>("/orderHub");

app.Run();