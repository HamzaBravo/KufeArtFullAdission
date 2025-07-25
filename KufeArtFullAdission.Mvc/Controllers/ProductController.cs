using AppDbContext;
using KufeArtFullAdission.Entity;
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
public class ProductController(DBContext _dbContext, IImageService _imageService) : Controller
{

    public async Task<IActionResult> Index()
    {
        var products = await _dbContext.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.CategoryName)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return View(products);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.ExistingCategories = await GetExistingCategories();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductDbEntity product, List<IFormFile> images)
    {
        try
        {
            // Küfe Point seçili değilse puanı sıfırla
            if (!product.HasKufePoints)
            {
                product.KufePoints = 0; // Checkbox seçili değilse puan 0 olsun
            }
            // Sadece checkbox seçili VE puan girilmişse validasyon yap
            else if (product.HasKufePoints && product.KufePoints <= 0)
            {
                ModelState.AddModelError("KufePoints", "Küfe Point aktifse puan 0'dan büyük olmalıdır!");
            }

            if (product.KufePoints > 10000)
            {
                ModelState.AddModelError("KufePoints", "Maksimum 10.000 puan verilebilir!");
            }

            // Mantık hatası kontrolü
            if (!product.HasKufePoints && product.KufePoints > 0)
            {
                product.KufePoints = 0; // Otomatik düzelt
            }

            // ✅ YENİ: Prim doğrulaması
            if (product.IsCommissionEligible && product.CommissionRate < 0)
            {
                ModelState.AddModelError("CommissionRate", "Prim oranı negatif olamaz!");
            }

            if (product.IsCommissionEligible && product.CommissionRate > 100)
            {
                ModelState.AddModelError("CommissionRate", "Prim oranı %100'den fazla olamaz!");
            }

            // Prim dahil değilse oranı sıfırla
            if (!product.IsCommissionEligible)
            {
                product.CommissionRate = 0;
            }

            if (ModelState.IsValid)
            {
                product.IsActive = true;
                product.Name = product.Name.ToUpper();
                product.CategoryName = product.CategoryName.ToUpper();
                _dbContext.Products.Add(product);
                await _dbContext.SaveChangesAsync();

                // Resim upload
                if (images != null && images.Any())
                {
                    var imagePaths = await _imageService.UploadImagesAsync(images, "products");

                    foreach (var imagePath in imagePaths)
                    {
                        var productImage = new ProductImagesDbEntity
                        {
                            ProductId = product.Id,
                            ImagePath = imagePath
                        };
                        _dbContext.ProductImages.Add(productImage);
                    }

                    await _dbContext.SaveChangesAsync();
                }

                TempData["ToastMessage"] = "Ürün başarıyla eklendi!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index");
            }
        }
        catch (Exception ex)
        {
            TempData["ToastMessage"] = "Hata: " + ex.Message;
            TempData["ToastType"] = "error";
        }

        ViewBag.ExistingCategories = await GetExistingCategories();
        return View(product);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null) return NotFound();

        ViewBag.ExistingCategories = await GetExistingCategories();
        ViewBag.ExistingImages = await _dbContext.ProductImages
            .Where(pi => pi.ProductId == id)
            .ToListAsync();

        return View(product);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProductDbEntity product, List<IFormFile> images, List<Guid> deleteImageIds)
    {
        try
        {
            if (product.HasKufePoints && product.KufePoints <= 0)
            {
                ModelState.AddModelError("KufePoints", "Küfe Point aktifse puan 0'dan büyük olmalıdır!");
            }

            if (product.KufePoints > 10000)
            {
                ModelState.AddModelError("KufePoints", "Maksimum 10.000 puan verilebilir!");
            }

            if (!product.HasKufePoints && product.KufePoints > 0)
            {
                product.KufePoints = 0;
            }

            // ✅ YENİ: Prim doğrulaması
            if (product.IsCommissionEligible && product.CommissionRate < 0)
            {
                ModelState.AddModelError("CommissionRate", "Prim oranı negatif olamaz!");
            }

            if (product.IsCommissionEligible && product.CommissionRate > 100)
            {
                ModelState.AddModelError("CommissionRate", "Prim oranı %100'den fazla olamaz!");
            }

            // Prim dahil değilse oranı sıfırla
            if (!product.IsCommissionEligible)
            {
                product.CommissionRate = 0;
            }

            if (ModelState.IsValid)
            {
                product.Name = product.Name.ToUpper();
                product.CategoryName = product.CategoryName.ToUpper();

                _dbContext.Products.Update(product);

                // Silinecek resimleri işle
                if (deleteImageIds != null && deleteImageIds.Any())
                {
                    var imagesToDelete = await _dbContext.ProductImages
                        .Where(pi => deleteImageIds.Contains(pi.Id))
                        .ToListAsync();

                    if (imagesToDelete.Any())
                    {
                        var imagePaths = imagesToDelete.Select(pi => pi.ImagePath).ToList();
                        await _imageService.DeleteImagesAsync(imagePaths);
                        _dbContext.ProductImages.RemoveRange(imagesToDelete);
                    }
                }

                // Yeni resimleri ekle
                if (images != null && images.Any())
                {
                    var imagePaths = await _imageService.UploadImagesAsync(images, "products");

                    foreach (var imagePath in imagePaths)
                    {
                        var productImage = new ProductImagesDbEntity
                        {
                            ProductId = product.Id,
                            ImagePath = imagePath
                        };
                        _dbContext.ProductImages.Add(productImage);
                    }
                }

                await _dbContext.SaveChangesAsync();

                TempData["ToastMessage"] = "Ürün başarıyla güncellendi!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index");
            }
        }
        catch (Exception ex)
        {
            TempData["ToastMessage"] = "Hata: " + ex.Message;
            TempData["ToastType"] = "error";
        }

        ViewBag.ExistingCategories = await GetExistingCategories();
        ViewBag.ExistingImages = await _dbContext.ProductImages
            .Where(pi => pi.ProductId == product.Id)
            .ToListAsync();

        return View(product);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var product = await _dbContext.Products.FindAsync(id);
            if (product != null)
            {
                // Resimleri sil
                var productImages = await _dbContext.ProductImages
                    .Where(pi => pi.ProductId == id)
                    .ToListAsync();

                if (productImages.Any())
                {
                    var imagePaths = productImages.Select(pi => pi.ImagePath).ToList();
                    await _imageService.DeleteImagesAsync(imagePaths);
                    _dbContext.ProductImages.RemoveRange(productImages);
                }

                product.IsActive = false;
                await _dbContext.SaveChangesAsync();

                return Json(new { success = true, message = "Ürün başarıyla silindi!" });
            }

            return Json(new { success = false, message = "Ürün bulunamadı!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    private async Task<List<string>> GetExistingCategories()
    {
        var categories = await _dbContext.Products
            .Where(p => p.IsActive && !string.IsNullOrEmpty(p.CategoryName))
            .Select(p => p.CategoryName)
            .ToListAsync();

        return categories.Distinct().OrderBy(c => c).ToList();
    }
}