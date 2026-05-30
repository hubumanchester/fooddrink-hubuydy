namespace FoodVisionMauiDemo.Services
{
    public class ImageStorageService
    {
        public async Task<string> SaveImageAsync(byte[] imageBytes, string? fileName = null, CancellationToken cancellationToken = default)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));

            var imagesFolder = GetImagesFolder();
            Directory.CreateDirectory(imagesFolder);

            var safeFileName = string.IsNullOrWhiteSpace(fileName)
                ? $"scan_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.jpg"
                : fileName;

            var imagePath = Path.Combine(imagesFolder, safeFileName);
            await File.WriteAllBytesAsync(imagePath, imageBytes, cancellationToken);

            return imagePath;
        }

        public async Task<string> EnsureImageInAppDataAsync(string? imagePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                return string.Empty;

            var imagesFolder = GetImagesFolder();
            Directory.CreateDirectory(imagesFolder);

            var fullImagePath = Path.GetFullPath(imagePath);
            var fullImagesFolder = Path.GetFullPath(imagesFolder);
            if (fullImagePath.StartsWith(fullImagesFolder, StringComparison.OrdinalIgnoreCase))
                return imagePath;

            var extension = Path.GetExtension(imagePath);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            var destinationPath = Path.Combine(imagesFolder, $"scan_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}");

            await using var source = File.OpenRead(imagePath);
            await using var destination = File.Create(destinationPath);
            await source.CopyToAsync(destination, cancellationToken);

            return destinationPath;
        }

        private static string GetImagesFolder()
        {
            return Path.Combine(FileSystem.Current.AppDataDirectory, "images");
        }
    }
}
