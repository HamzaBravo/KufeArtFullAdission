using AppDbContext;
using KufeArt.TabletMvc.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KufeArt.TabletMvc.Controllers;

public class HomeController (DBContext _dbContex) : Controller
{
    private const string TABLET_PASSWORD = "küfeart2025";
    // 🔐 LOGIN SAYFASI
    [HttpGet]
    public IActionResult Login()
    {
        // Zaten giriş yapmışsa dashboard'a yönlendir
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard");
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(TabletLoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // ✅ 1. ŞİFRE KONTROLÜ
        if (model.Password != TABLET_PASSWORD)
        {
            ModelState.AddModelError("Password", "Geçersiz şifre!");
            return View(model);
        }

        // 2. Geçerli departman kontrolü
        if (model.Department != "Kitchen" && model.Department != "Bar")
        {
            ModelState.AddModelError("Department", "Geçersiz departman seçimi");
            return View(model);
        }

        try
        {
            // Claims oluştur
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.TabletName),
                new Claim("Department", model.Department),
                new Claim("LoginTime", DateTime.Now.ToString()),
                new Claim("TabletId", Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrEmpty(model.Note))
            {
                claims.Add(new Claim("Note", model.Note));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Dashboard");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Giriş işlemi sırasında bir hata oluştu");
            return View(model);
        }
    }

    // 📊 DASHBOARD
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var department = User.FindFirst("Department")?.Value;
        var tabletName = User.Identity?.Name;

        if (string.IsNullOrEmpty(department) || string.IsNullOrEmpty(tabletName))
        {
            return RedirectToAction("Login");
        }

        var sessionModel = new TabletSessionModel
        {
            Department = department,
            TabletName = tabletName,
            LoginTime = DateTime.Parse(User.FindFirst("LoginTime")?.Value ?? DateTime.Now.ToString()),
            Note = User.FindFirst("Note")?.Value
        };

        var model = new OrderDashboardModel
        {
            Session = sessionModel,
            Orders = new List<TabletOrderModel>(), // İlk yüklemede boş, AJAX ile doldurulacak
            Stats = new DashboardStatsModel()
        };

        return View(model);
    }

    // 🚪 LOGOUT
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // 🔄 SESSION KONTROLÜ
    [Authorize]
    [HttpGet]
    public IActionResult GetSessionInfo()
    {
        var sessionModel = new TabletSessionModel
        {
            Department = User.FindFirst("Department")?.Value ?? "",
            TabletName = User.Identity?.Name ?? "",
            LoginTime = DateTime.Parse(User.FindFirst("LoginTime")?.Value ?? DateTime.Now.ToString()),
            Note = User.FindFirst("Note")?.Value
        };

        return Json(new { success = true, data = sessionModel });
    }
}