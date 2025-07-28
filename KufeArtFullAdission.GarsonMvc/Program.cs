using AppDbContext;
using KufeArtFullAdission.GarsonMvc.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSignalR();


builder.Services.AddHostedService<InactiveTableMonitorService>();

// 🎯 DATABASE
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HamzaLocal")));

// 🔐 AUTHENTICATION
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.Cookie.Name = "GarsonAuth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// 🌐 SIGNALR CLIENT
builder.Services.AddSignalR();

builder.Services.AddHttpClient("TabletPanel", client =>
{
    var tabletUrl = builder.Environment.IsDevelopment()
        ? "https://localhost:7051/"  // TabletMvc'nin çalıştığı port
        : "https://tablet.kufeart.com";
    client.BaseAddress = new Uri(tabletUrl);
});


// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("AdminPanel", client =>
{
    var adminUrl = builder.Environment.IsDevelopment()
        ? "https://localhost:7164"
        : "https://adisyon.kufeart.com";
    client.BaseAddress = new Uri(adminUrl);
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapHub<WaiterHub>("/waiterHub");

app.Run();
