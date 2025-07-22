using KufeArtFullAdission.Mvc.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
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
                var fileName = $"{Guid.NewGuid()}.webp";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var image = await Image.LoadAsync(file.OpenReadStream());
                await image.SaveAsWebpAsync(filePath);

                // ✅ DÜZELTME: Her zaman "/" ile başlayan tam yol
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