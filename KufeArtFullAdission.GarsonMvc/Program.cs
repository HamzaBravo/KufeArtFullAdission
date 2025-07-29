using AppDbContext;
using KufeArtFullAdission.GarsonMvc.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHostedService<InactiveTableMonitorService>();

// Database
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HamzaLocal")));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.Cookie.Name = "GarsonAuth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// ✅ YENİ: CORS ekleme (TabletMvc'den gelen istekler için)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7051", "https://tablet.kufeart.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();

// HTTP Clients
builder.Services.AddHttpClient("TabletPanel", client =>
{
    var tabletUrl = builder.Environment.IsDevelopment()
        ? "https://localhost:7051/"
        : "https://tablet.kufeart.com";
    client.BaseAddress = new Uri(tabletUrl);
});

builder.Services.AddHttpClient("AdminPanel", client =>
{
    var adminUrl = builder.Environment.IsDevelopment()
        ? "https://localhost:7164"
        : "https://adisyon.kufeart.com";
    client.BaseAddress = new Uri(adminUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ✅ YENİ: CORS middleware
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<WaiterHub>("/waiterHub");

app.Run();