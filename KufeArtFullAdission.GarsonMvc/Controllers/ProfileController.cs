// KufeArtFullAdission.GarsonMvc/Controllers/ProfileController.cs
using AppDbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KufeArtFullAdission.GarsonMvc.Controllers;

[Authorize]
public class ProfileController(DBContext _dbContext) : Controller
{
    // Profil görüntüleme sayfası
    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _dbContext.Persons.FindAsync(userId);

            if (user == null || user.IsDeleted)
                return RedirectToAction("Login", "Auth");

            return View(user);
        }
        catch (Exception)
        {
            return RedirectToAction("Index", "Home");
        }
    }

    // Şifre değiştirme API
    [HttpPost]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _dbContext.Persons.FindAsync(userId);

            if (user == null || user.IsDeleted)
                return Json(new { success = false, message = "Kullanıcı bulunamadı!" });

            // Mevcut şifre kontrolü
            if (user.Password != request.CurrentPassword)
                return Json(new { success = false, message = "Mevcut şifre yanlış!" });

            // Yeni şifre validasyonu
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 4)
                return Json(new { success = false, message = "Yeni şifre en az 4 karakter olmalıdır!" });

            if (request.NewPassword == request.CurrentPassword)
                return Json(new { success = false, message = "Yeni şifre eskisiyle aynı olamaz!" });

            // Şifreyi güncelle
            user.Password = request.NewPassword;
            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Şifre başarıyla değiştirildi!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
        }
    }

    // Profil bilgilerini güncelleme
    [HttpPost]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _dbContext.Persons.FindAsync(userId);

            if (user == null || user.IsDeleted)
                return Json(new { success = false, message = "Kullanıcı bulunamadı!" });

            // Validasyon
            if (string.IsNullOrWhiteSpace(request.FullName))
                return Json(new { success = false, message = "Ad soyad boş olamaz!" });

            // Güncelle
            user.FullName = request.FullName.ToUpper();
            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Profil bilgileri güncellendi!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
        }
    }
}

// DTO Models
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = "";
}