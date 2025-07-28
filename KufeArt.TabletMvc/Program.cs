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

// MVC Services
builder.Services.AddControllersWithViews();


builder.Services.AddHttpClient();


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

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.MapHub<TabletHub>("/tabletHub");

app.Run();