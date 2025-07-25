using KufeArtFullAdission.Mvc.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Services;

public class ImageService : IImageService
{
    private readonly IWebHostEnvironment _environment;

    public ImageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<List<string>> UploadImagesAsync(List<IFormFile> files, string folderName)
    {
        var uploadedPaths = new List<string>();
        if (files == null || !files.Any()) return uploadedPaths;

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folderName);
        Directory.CreateDirectory(uploadsFolder);

        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                // ✅ YENİ: Dosya boyutu kontrolü
                if (file.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    throw new InvalidOperationException($"Dosya çok büyük: {file.FileName}. Maksimum 10MB olmalı.");
                }

                var fileName = $"{Guid.NewGuid()}.webp";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var image = await Image.LoadAsync(file.OpenReadStream());

                // ✅ YENİ: Resim boyutunu optimize et
                if (image.Width > 1920 || image.Height > 1920)
                {
                    var ratio = Math.Min(1920.0 / image.Width, 1920.0 / image.Height);
                    var newWidth = (int)(image.Width * ratio);
                    var newHeight = (int)(image.Height * ratio);
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                }

                // ✅ YENİ: WebP kalitesi ayarı
                var encoder = new WebpEncoder()
                {
                    Quality = 85 // %85 kalite - dosya boyutunu küçültür
                };

                await image.SaveAsync(filePath, encoder);

                var webPath = $"/uploads/{folderName}/{fileName}";
                uploadedPaths.Add(webPath);
            }
        }

        return uploadedPaths;
    }

    public async Task<bool> DeleteImagesAsync(List<string> imagePaths)
    {
        try
        {
            foreach (var imagePath in imagePaths)
            {
                if (!string.IsNullOrEmpty(imagePath))
                {
                    // ✅ DÜZELTME: Baştan "/" karakterini temizle
                    var cleanPath = imagePath.TrimStart('/');
                    var physicalPath = Path.Combine(_environment.WebRootPath, cleanPath);

                    if (File.Exists(physicalPath))
                    {
                        File.Delete(physicalPath);
                    }
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}