using AppDbContext;
using KufeArt.TabletMvc.Hubs;
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

// 📞 HTTP CLIENT (Tablet paneli ile iletişim)
builder.Services.AddHttpClient("TabletPanel", client =>
{
    var tabletUrl = builder.Environment.IsDevelopment()
        ? "https://localhost:7051"  // TabletMvc port'u
        : "https://tablet.kufeart.com";  // Production tablet URL'i
    client.BaseAddress = new Uri(tabletUrl);
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


app.MapHub<TabletHub>("/tabletHub");

app.Run();