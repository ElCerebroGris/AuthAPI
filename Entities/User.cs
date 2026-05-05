using Microsoft.AspNetCore.Identity;

namespace AuthAPI.Entities
{
    public class User : IdentityUser
    {
        public int? Order { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AccountType { get; set; }
        public string? IUF { get; set; }
        public string? BotpressKey { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
