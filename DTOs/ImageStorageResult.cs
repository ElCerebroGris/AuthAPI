namespace AuthAPI.DTOs
{
    public class ImageStorageResult
    {
        public bool IsSuccess { get; init; }
        public string? PublicUrl { get; init; }
        public string? FileIdentifier { get; init; }
        public string? ErrorMessage { get; init; }

        public static ImageStorageResult Success(string url, string identifier)
            => new() { IsSuccess = true, PublicUrl = url, FileIdentifier = identifier };

        public static ImageStorageResult Failure(string error)
            => new() { IsSuccess = false, ErrorMessage = error };
    }
}
