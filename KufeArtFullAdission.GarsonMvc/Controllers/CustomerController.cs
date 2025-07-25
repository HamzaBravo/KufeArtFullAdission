// KufeArtFullAdission.GarsonMvc/Controllers/CustomerController.cs
using AppDbContext;
using KufeArtFullAdission.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KufeArtFullAdission.GarsonMvc.Controllers;

[Authorize]
public class CustomerController(DBContext _dbContext) : Controller
{
    // Müşteri kayıt sayfası
    public IActionResult Create()
    {
        return View();
    }

    // Müşteri kayıt işlemi
    [HttpPost]
    public async Task<IActionResult> Create(CustomerDbEntity customer)
    {
        try
        {
            // Telefon numarası benzersizlik kontrolü
            var existingCustomer = await _dbContext.Customers
                .AnyAsync(c => c.PhoneNumber == customer.PhoneNumber);

            if (existingCustomer)
            {
                return Json(new { success = false, message = "Bu telefon numarası zaten kayıtlı!" });
            }

            // Validasyon
            if (string.IsNullOrWhiteSpace(customer.Fullname) ||
                string.IsNullOrWhiteSpace(customer.PhoneNumber) ||
                string.IsNullOrWhiteSpace(customer.Password))
            {
                return Json(new { success = false, message = "Tüm alanlar zorunludur!" });
            }

            if (customer.Password.Length < 4)
            {
                return Json(new { success = false, message = "Şifre en az 4 karakter olmalıdır!" });
            }

            // Müşteriyi kaydet
            customer.IsActive = true;
            customer.Fullname = customer.Fullname.ToUpper();

            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Müşteri başarıyla kaydedildi!",
                customer = new
                {
                    id = customer.Id,
                    name = customer.Fullname,
                    phone = customer.PhoneNumber
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Hata: " + ex.Message });
        }
    }

    // Müşteri arama
    [HttpGet]
    public async Task<IActionResult> Search(string phone)
    {
        try
        {
            if (string.IsNullOrEmpty(phone))
                return Json(new { success = false, message = "Telefon numarası gerekli!" });

            var customer = await _dbContext.Customers
                .Where(c => c.PhoneNumber.Contains(phone) && c.IsActive)
                .Select(c => new {
                    id = c.Id,
                    name = c.Fullname,
                    phone = c.PhoneNumber
                })
                .ToListAsync();

            return Json(new { success = true, customers = customer });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}