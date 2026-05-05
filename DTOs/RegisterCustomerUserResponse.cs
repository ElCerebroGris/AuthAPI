namespace AuthAPI.DTOs
{
    public class RegisterCustomerUserResponse
    {
        public string Id { get; internal set; }
        public int? Order { get; internal set; }
        public string? IUF { get; internal set; }
        public string? BotpressKey { get; internal set; }
        public int Experies_in { get; internal set; }
        public bool IsActive { get; internal set; }
        public string Token { get; internal set; }
    }
}
