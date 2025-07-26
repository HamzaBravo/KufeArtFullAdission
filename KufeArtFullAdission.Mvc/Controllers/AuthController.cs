using AppDbContext;
using KufeArtFullAdission.Enums;
using KufeArtFullAdission.Mvc.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Controllers;

public class AuthController(DBContext _dbContext) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Zaten giriş yapmışsa dashboard'a yönlendir
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        try
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["ToastMessage"] = "Kullanıcı adı ve şifre gereklidir!";
                TempData["ToastType"] = "error";
                return View();
            }

            // Kullanıcı doğrulama
            var user = await _dbContext.Persons
                .FirstOrDefaultAsync(p =>
                    p.Username == username &&
                    p.Password == password &&
                    p.IsActive &&
                    !p.IsDeleted
                    &&p.AccessType==AccessType.Admin);

            if (user == null)
            {
                TempData["ToastMessage"] = "Geçersiz kullanıcı adı veya şifre!";
                TempData["ToastType"] = "error";
                return View();
            }

            // Claims oluştur
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new("Username", user.Username),
                new(ClaimTypes.Role, user.AccessType.ToString()),
                new("ProfileImage", user.ProfileImagePath ?? ""),
                new("IsSuperAdmin", (user.IsSuperAdmin == true).ToString()) // 🆕 Yeni claim
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // "Beni Hatırla" özelliği
                ExpiresUtc = DateTimeOffset.Now.AddDays(7) // 7 gün geçerli
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["ToastMessage"] = $"Hoş geldiniz, {user.FullName}!";
            TempData["ToastType"] = "success";

            // Return URL varsa oraya, yoksa dashboard'a yönlendir
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["ToastMessage"] = "Giriş işlemi sırasında hata oluştu!";
            TempData["ToastType"] = "error";
            return View();
        }
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["ToastMessage"] = "Güvenli çıkış yapıldı!";
        TempData["ToastType"] = "info";

        return RedirectToAction("Login");
    }

    [Authorize]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _dbContext.Persons.FindAsync(userId);

            if (user == null || user.Password != request.CurrentPassword)
            {
                return Json(new { success = false, message = "Mevcut şifre yanlış!" });
            }

            user.Password = request.NewPassword;
            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Şifre başarıyla değiştirildi!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Hata oluştu!" });
        }
    }
}