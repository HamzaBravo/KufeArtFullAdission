using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Interfaces;

public interface IImageService
{
    Task<List<string>> UploadImagesAsync(List<IFormFile> files, string folderName);
    Task<bool> DeleteImagesAsync(List<string> imagePaths);
}
