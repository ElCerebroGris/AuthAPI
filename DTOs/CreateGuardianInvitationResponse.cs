namespace AuthAPI.DTOs
{
    public class CreateGuardianInvitationResponse
    {
        public Guid InvitationId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool SmsSent { get; set; }
        public string GuardianPhoneNumber { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
