using AppDbContext;
using KufeArtFullAdission.Entity;
using KufeArtFullAdission.Enums;
using KufeArtFullAdission.Mvc.Interfaces;
using KufeArtFullAdission.Mvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Controllers;

[Authorize]
public class PersonController(DBContext _dbContext, IImageService _imageService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var persons = await _dbContext.Persons
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.AccessType)
            .ThenBy(p => p.FullName)
            .ToListAsync();

        return View(persons);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(PersonDbEntity person, IFormFile profileImage)
    {
        try
        {
            // Username benzersizlik kontrolü
            var existingUser = await _dbContext.Persons
                .AnyAsync(p => p.Username == person.Username && !p.IsDeleted);

            if (existingUser)
            {
                ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılıyor!");
            }

            if (ModelState.IsValid)
            {
                person.IsActive = true;
                person.IsDeleted = false;

                person.FullName= person.FullName.ToUpper();
                person.Username = person.Username.ToLower();

                // Profil resmi upload
                if (profileImage != null && profileImage.Length > 0)
                {
                    var imagePaths = await _imageService.UploadImagesAsync(new List<IFormFile> { profileImage }, "profiles");
                    if (imagePaths.Any())
                    {
                        person.ProfileImagePath = imagePaths.First();
                    }
                }

                _dbContext.Persons.Add(person);
                await _dbContext.SaveChangesAsync();

                TempData["ToastMessage"] = "Personel başarıyla eklendi!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index");
            }
        }
        catch (Exception ex)
        {
            TempData["ToastMessage"] = "Hata: " + ex.Message;
            TempData["ToastType"] = "error";
        }

        return View(person);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var person = await _dbContext.Persons.FindAsync(id);
        if (person == null || person.IsDeleted) return NotFound();

        return View(person);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(PersonDbEntity person, IFormFile profileImage, bool deleteExistingImage)
    {
        try
        {
            var existingPerson = await _dbContext.Persons.FindAsync(person.Id);
            if (existingPerson == null) return NotFound();

            // Username benzersizlik kontrolü (kendisi hariç)
            var existingUser = await _dbContext.Persons
                .AnyAsync(p => p.Username == person.Username && p.Id != person.Id && !p.IsDeleted);

            if (existingUser)
            {
                ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılıyor!");
            }

            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(person.Password))
                {
                    person.Password = existingPerson.Password; // Mevcut şifreyi koru
                }

                // Mevcut resmi sil
                if (deleteExistingImage && !string.IsNullOrEmpty(existingPerson.ProfileImagePath))
                {
                    await _imageService.DeleteImagesAsync(new List<string> { existingPerson.ProfileImagePath });
                    person.ProfileImagePath = null;
                }
                else if (profileImage == null || profileImage.Length == 0)
                {
                    // Yeni resim yoksa mevcut resmi koru
                    person.ProfileImagePath = existingPerson.ProfileImagePath;
                }

                // Yeni resim upload
                if (profileImage != null && profileImage.Length > 0)
                {
                    // Eski resmi sil
                    if (!string.IsNullOrEmpty(existingPerson.ProfileImagePath))
                    {
                        await _imageService.DeleteImagesAsync(new List<string> { existingPerson.ProfileImagePath });
                    }

                    var imagePaths = await _imageService.UploadImagesAsync(new List<IFormFile> { profileImage }, "profiles");
                    if (imagePaths.Any())
                    {
                        person.ProfileImagePath = imagePaths.First();
                    }
                }



                // Diğer alanları güncelle
                person.CreatedAt = existingPerson.CreatedAt;
                person.FullName= person.FullName.ToUpper();
                person.Username = person.Username.ToUpper();



                _dbContext.Entry(existingPerson).CurrentValues.SetValues(person);


                await _dbContext.SaveChangesAsync();

                TempData["ToastMessage"] = "Personel başarıyla güncellendi!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index");
            }
        }
        catch (Exception ex)
        {
            TempData["ToastMessage"] = "Hata: " + ex.Message;
            TempData["ToastType"] = "error";
        }

        return View(person);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var person = await _dbContext.Persons.FindAsync(id);
            if (person != null)
            {
                // Profil resmini sil
                if (!string.IsNullOrEmpty(person.ProfileImagePath))
                {
                    await _imageService.DeleteImagesAsync(new List<string> { person.ProfileImagePath });
                }

                person.IsDeleted = true; // Soft delete
                person.IsActive = false;
                await _dbContext.SaveChangesAsync();

                return Json(new { success = true, message = "Personel başarıyla silindi!" });
            }

            return Json(new { success = false, message = "Personel bulunamadı!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}