using AppDbContext;
using KufeArtFullAdission.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Controllers;

public class TableController(DBContext _dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var tables = await _dbContext.Tables
            .Where(t => t.IsActive)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return View(tables);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.ExistingCategories = await GetExistingCategories();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(TableDbEntity table)
    {
        try
        {
            if (ModelState.IsValid)
            {
                table.IsActive = true;
                _dbContext.Tables.Add(table);
                await _dbContext.SaveChangesAsync();

                TempData["ToastMessage"] = "Masa başarıyla eklendi!";
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
        return View(table);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var table = await _dbContext.Tables.FindAsync(id);
        if (table == null) return NotFound();

        ViewBag.ExistingCategories = await GetExistingCategories();
        return View(table);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(TableDbEntity table)
    {
        try
        {
            if (ModelState.IsValid)
            {
                _dbContext.Tables.Update(table);
                await _dbContext.SaveChangesAsync();

                TempData["ToastMessage"] = "Masa başarıyla güncellendi!";
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
        return View(table);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var table = await _dbContext.Tables.FindAsync(id);
            if (table != null)
            {
                table.IsActive = false; // Soft delete
                await _dbContext.SaveChangesAsync();

                return Json(new { success = true, message = "Masa başarıyla silindi!" });
            }

            return Json(new { success = false, message = "Masa bulunamadı!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }


    private async Task<List<string>> GetExistingCategories()
    {
        return await _dbContext.Tables
            .Where(t => t.IsActive && !string.IsNullOrEmpty(t.Category))
            .Select(t => t.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }
}