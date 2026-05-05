using AuthAPI.DTOs;
using Microsoft.Extensions.Options;
using Supabase;

namespace AuthAPI.Services
{
    public sealed partial class SupabaseImageStorageService
    {
        private readonly Client _storageClient;
        private readonly SupabaseSettings _settings;
        private readonly ILogger<SupabaseImageStorageService> _logger;

        public SupabaseImageStorageService(IOptions<SupabaseSettings> options, ILogger<SupabaseImageStorageService> logger)
        {
            _logger = logger;
            _settings = options.Value;
            _storageClient = new Client(_settings.Url, _settings.ApiKey);
        }

        public async Task<ImageStorageResult> UploadImageBytesAsync(IFormFile file, string bucketKey)
        {
            if (!_settings.Buckets.ContainsKey(bucketKey))
                throw new ArgumentException($"Bucket '{bucketKey}' não está configurado.");

            try
            {

                var sanitizedFileName = MyRegex().Replace(file.FileName, "");
                var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}.{file.ContentType.Split('/')[1]}"; // Use the file's content type to determine the extension

                var bucketName = _settings.Buckets[bucketKey];

                Console.WriteLine($"Uploading image to bucket: {bucketName}, file: {uniqueFileName}");

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                byte[] fileBytes = memoryStream.ToArray();

                await _storageClient.Storage
                    .From(bucketName)
                    .Upload(fileBytes, uniqueFileName, new Supabase.Storage.FileOptions
                    {
                        ContentType = file.ContentType,
                        Upsert = true // Overwrite if file already exists
                    });

                var publicUrl = $"{_settings.Url}/storage/v1/object/public/{bucketName}/{uniqueFileName}";

                return ImageStorageResult.Success(publicUrl, uniqueFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image bytes to Supabase");
                Console.WriteLine(ex.Message);
                return ImageStorageResult.Failure(ex.Message);
            }
        }

        [System.Text.RegularExpressions.GeneratedRegex(@"[^a-zA-Z0-9_\-]")]
        private static partial System.Text.RegularExpressions.Regex MyRegex();
    }
}
