namespace AuthAPI.DTOs
{
    public class SupabaseSettings
    {
        public string Url { get; set; }
        public string ApiKey { get; set; }
        public Dictionary<string, string> Buckets { get; set; }
    }
}
