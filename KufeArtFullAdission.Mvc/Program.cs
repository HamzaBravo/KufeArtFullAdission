using AppDbContext;
using KufeArtFullAdission.Mvc.Hubs;
using KufeArtFullAdission.Mvc.Interfaces;
using KufeArtFullAdission.Mvc.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


builder.Services.AddSignalR();

// Mevcut kodlarýnýn altýna ekle
builder.Services.AddScoped<IImageService, ImageService>();

// DBContext yapýlandýrmasý
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HamzaLocal")), ServiceLifetime.Scoped);

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name = "KufeArtAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });


// Program.cs builder.Services kýsmýna ekleyin:
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
    options.ValueLengthLimit = 50 * 1024 * 1024; // 50MB
    options.ValueCountLimit = 100;
});

// Web server konfigürasyonu için
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7115", "https://garson.kufeart.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


builder.Services.AddHttpClient("WaiterPanel", client =>
{
    var waiterUrl = builder.Environment.IsDevelopment()
        ? "https://localhost:7115"
        : "https://garson.kufeart.com";
    client.BaseAddress = new Uri(waiterUrl);
});

builder.Services.AddAuthorization();

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

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(@"C:\SharedUploads"),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseCors();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();


app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Robots-Tag", "noindex, nofollow");
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapHub<OrderHub>("/orderHub");

app.Run();
