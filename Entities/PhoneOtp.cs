using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Entities
{
    public class PhoneOtp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string PhoneNumber { get; set; }
        public string Code { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
