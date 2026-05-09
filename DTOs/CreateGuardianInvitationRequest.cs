using System.ComponentModel.DataAnnotations;

namespace AuthAPI.DTOs
{
    public class CreateGuardianInvitationRequest
    {
        [Required]
        [MaxLength(20)]
        public string GuardianPhoneNumber { get; set; } = null!;

        [MaxLength(160)]
        public string? GuardianName { get; set; }

        [MaxLength(30)]
        public string? RelationshipType { get; set; }
    }
}
