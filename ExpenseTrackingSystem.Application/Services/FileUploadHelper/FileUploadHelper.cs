using Microsoft.AspNetCore.Http;

namespace SpendwiseSystem.Application.Services.FileUploadHelper
{
    public static class FileUploadHelper
    {
        public static async Task<string> UploadFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var pdfExtensions = new[] { ".pdf" };

            string folderName;

            if (imageExtensions.Contains(extension))
            {
                folderName = "images";
            }
            else if (pdfExtensions.Contains(extension))
            {
                folderName = "pdf";
            }
            else
            {
                throw new Exception("Only image and pdf files are allowed.");
            }

            var uploadsRoot = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                folderName
            );

            if (!Directory.Exists(uploadsRoot))
                Directory.CreateDirectory(uploadsRoot);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(uploadsRoot, uniqueFileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return uniqueFileName;
        }
        /* public static async Task<string?> UploadFileAsync(IFormFile? file, string webRootPath)
         {
             if (file == null || file.Length == 0)
                 return null;

             var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

             var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
             var pdfExtensions = new[] { ".pdf" };

             string folderName;

             if (imageExtensions.Contains(extension))
             {
                 folderName = "images";
             }
             else if (pdfExtensions.Contains(extension))
             {
                 folderName = "pdf";
             }
             else
             {
                 throw new Exception("Only image and pdf files are allowed.");
             }

             var uploadsRoot = Path.Combine(webRootPath, "uploads", folderName);

             if (!Directory.Exists(uploadsRoot))
                 Directory.CreateDirectory(uploadsRoot);

             var uniqueFileName = $"{Guid.NewGuid()}{extension}";
             var fullPath = Path.Combine(uploadsRoot, uniqueFileName);

             using var stream = new FileStream(fullPath, FileMode.Create);
             await file.CopyToAsync(stream);

             return uniqueFileName;
         }*/


        public static bool DeleteUploadedFile(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return false;

                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                var folderPath = extension == ".pdf"
                    ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pdf")
                    : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "images");

                var fullPath = Path.Combine(folderPath, fileName);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static List<string> GetFileUrl(List<string?> fileNames, HttpRequest request)
        {
            if (fileNames == null || fileNames.Count == 0)
                return null;

            var fileUrls = new List<string>();
            foreach (var fileName in fileNames)
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    continue;

                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                string folderName = GetFolderType(extension);
                fileUrls.Add($"{request.Scheme}://{request.Host}/uploads/{folderName}/{fileName}");
            }
            return fileUrls;
        }

        public static string GetFolderType(string extension)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var pdfExtensions = new[] { ".pdf" };


            if (imageExtensions.Contains(extension))
            {
                return "images";
            }
            else if (pdfExtensions.Contains(extension))
            {
                return "pdf";
            }
            else
            {
                return null;
            }
        }

        public static bool IsFileExists(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var folderPath = extension == ".pdf"
                ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pdf")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "images");
            var fullPath = Path.Combine(folderPath, fileName);
            return File.Exists(fullPath);
        }
    }
}
