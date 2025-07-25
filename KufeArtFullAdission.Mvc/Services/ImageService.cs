using KufeArtFullAdission.Mvc.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Services
{
    public class ImageService : IImageService
    {
        private readonly string _physicalRoot;
        private readonly string _webRootPrefix;

        public ImageService(IConfiguration configuration)
        {
            // appsettings.json'dan yolları al
            _physicalRoot = configuration["UploadSettings:PhysicalRoot"];       // Örn: C:\SharedUploads
            _webRootPrefix = configuration["UploadSettings:WebRootPrefix"];     // Örn: /uploads
        }

        public async Task<List<string>> UploadImagesAsync(List<IFormFile> files, string folderName)
        {
            var uploadedPaths = new List<string>();
            if (files == null || !files.Any()) return uploadedPaths;

            var uploadsFolder = Path.Combine(_physicalRoot, folderName);
            Directory.CreateDirectory(uploadsFolder); // klasör yoksa oluştur

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    if (file.Length > 10 * 1024 * 1024)
                        throw new InvalidOperationException($"Dosya çok büyük: {file.FileName}. Maksimum 10MB olmalı.");

                    var fileName = $"{Guid.NewGuid()}.webp";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using var image = await Image.LoadAsync(file.OpenReadStream());

                    if (image.Width > 1920 || image.Height > 1920)
                    {
                        var ratio = Math.Min(1920.0 / image.Width, 1920.0 / image.Height);
                        var newWidth = (int)(image.Width * ratio);
                        var newHeight = (int)(image.Height * ratio);
                        image.Mutate(x => x.Resize(newWidth, newHeight));
                    }

                    var encoder = new WebpEncoder { Quality = 85 };
                    await image.SaveAsync(filePath, encoder);

                    var webPath = $"{_webRootPrefix}/{folderName}/{fileName}";
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
                        var cleanPath = imagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());

                        // Kök dizin ile birleştir
                        var physicalPath = Path.Combine(_physicalRoot, cleanPath.Replace(_webRootPrefix.Trim('/'), ""));

                        if (File.Exists(physicalPath))
                            File.Delete(physicalPath);
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
}
