using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Entities
{
    [Table("customers")]
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string? _id { get; set; }

        [Column("first_name")]
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [Column("sur_name")]
        [MaxLength(100)]
        public string? SurName { get; set; }

        [Column("device_token")]
        public string? DeviceToken { get; set; }

        [Column("device_type")]
        public string? DeviceType { get; set; }

        [Column("email")]
        [MaxLength(200)]
        public string? Email { get; set; }

        [Column("phone_number")]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Column("billing_address")]
        public string? BillingAddress { get; set; }

        [Column("shipping_address")]
        public string? ShippingAddress { get; set; }

        [Column("profile")]
        public string? Profile { get; set; }

        [Column("pin_code")]
        public string? PinCode { get; set; }

        [Column("wallet_balance")]
        public decimal WalletBalance { get; set; }


        // Backing field for MongoDate
        private string _mongoCreatedAt;

        [JsonProperty("createdAt")] // Maps from JSON field
        [NotMapped] // Prevents EF from creating a separate column
        public MongoDate? MongoDate { get; set; }

        [Column("mongo_createdAt")]
        public string? MongoCreatedAt
        {
            get => _mongoCreatedAt ?? MongoDate?.Date;
            set => _mongoCreatedAt = value;
        }

        // Backing field for MongoDate2
        private string _mongoUpdatedAt;

        [JsonProperty("updatedAt")] // Maps from JSON field
        [NotMapped] // Prevents EF from creating a separate column
        public MongoDate? MongoDate2 { get; set; }

        [Column("mongo_updatedAt")]
        public string? MongoUpdatedAt
        {
            get => _mongoUpdatedAt ?? MongoDate2?.Date;
            set => _mongoUpdatedAt = value;
        }

        [Column("registration_number")]
        public string? RegistrationNumber { get; set; }

        [Column("nationality")]
        public string? Nationality { get; set; }

        [Column("dob")]
        public string? Dob { get; set; }

        [Column("gender")]
        public string? Gender { get; set; }

        [Column("identification_type")]
        public string? IdentificationType { get; set; }

        [Column("identification_number")]
        public string? IdentificationNumber { get; set; }

        [Column("wallet_number")]
        public string? WalletNumber { get; set; }

        [Column("bank_name")]
        public string? BankName { get; set; }

        [Column("iban")]
        public string? Iban { get; set; }

        [Column("identification_card_img")]
        public string? IdentificationCardImg { get; set; }

        [Column("proxpay_id")]
        public string? ProxpayId { get; set; }

        [Column("language")]
        [MaxLength(10)]
        public string? Language { get; set; }

        [Column("reg_number")]
        public int RegNumber { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        //[Column("is_document_account")]
        //public bool IsDocumentAccount { get; set; } = false;

        [Column("reference_no")]
        public string? ReferenceNo { get; set; }

        [ForeignKey("AspNetUsers")]
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public string? QrCodeUrl { get; set; }

        public bool IsDocumentAccount { get; set; }

        public string? MaritalStatus { get; set; }
        public string? IdentificationEmissionCountry { get; set; }
        public string? IdentificationExpiryDate { get; set; }
        public string? IdentificationEmissionDate { get; set; }
        public string? DiFrontalImage { get; set; }
        public string? DiBackImage { get; set; }
        public string? Province { get; set; }
        public string? Municipio { get; set; }
        public string? Country { get; set; }

        public string? BayqiTag { get; set; }
        public string? Pin { get; set; }

        // Dados específicos para menores
        public string? FathersName { get; set; }
        public string? MothersName { get; set; }
        public string? LegalRepresentativeType { get; set; }
        public string? LegalRepresentativeName { get; set; }
        public string? LegalRepresentativePhoneNumber { get; set; }
        public string? AddressNumber { get; set; }
        public int Number { get; internal set; }
        public string? ResidenceProvince { get; internal set; }
    }

    public class MongoDate
    {
        [JsonProperty("$date")]
        public string Date { get; set; }
    }
}
