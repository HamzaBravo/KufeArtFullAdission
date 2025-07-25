using AppDbContext;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KufeArtFullAdission.GarsonMvc.Controllers;

public class AuthController(DBContext _dbContext) : Controller
{
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string username, string password)
    {
        try
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Kullanıcı adı ve şifre gereklidir!";
                return View();
            }

            // ✅ Aktif tüm personeller giriş yapabilir (Admin/Garson fark etmez)
            var user = await _dbContext.Persons
                .FirstOrDefaultAsync(p =>
                    p.Username == username &&
                    p.Password == password &&
                    p.IsActive &&
                    !p.IsDeleted);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Geçersiz kullanıcı adı veya şifre!";
                return View();
            }

            // Claims oluştur
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new("Username", user.Username),
                new(ClaimTypes.Role, user.AccessType.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.Now.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Giriş işlemi sırasında hata oluştu!";
            return View();
        }
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}