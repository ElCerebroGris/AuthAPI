namespace AuthAPI.DTOs
{
    public class RegisterCustomerByBIRequest
    {
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string Nationality { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? MaritalStatus { get; set; }
        public string DiType { get; set; }
        public string DiNumber { get; set; }
        public string? DiEmissionCountry { get; set; }
        public DateTime? DiEmitionDate { get; set; }
        public DateTime DiExpiryDate { get; set; }
        public string? DiFrontalImage { get; set; }
        public IFormFile? DiFrontalImageFile { get; set; }
        public string? DiBackImage { get; set; }
        public IFormFile? DiBackImageFile { get; set; }
        public string Country { get; set; }
        public string Province { get; set; }
        public string Municipio { get; set; }
        public string Address { get; set; }
        public string? ResidenceProvince { get; set; }
        public string BayqiTag { get; set; }
        public string Password { get; set; }
        public string Pin { get; set; }
        public string? Profile { get; set; }
        public IFormFile? ProfileFile { get; set; }

        public string? FathersName { get; set; }
        public string? MothersName { get; set; }
        public string? LegalRepresentativeType { get; set; }
        public string? LegalRepresentativeName { get; set; }
        public string? LegalRepresentativePhoneNumber { get; set; }

        public RegisterCustomerByBIRequest ToRegisterCustomerUserDTO()
        {
            return new RegisterCustomerByBIRequest
            {
                PhoneNumber = null,
                Email = null,
                Nationality = Nationality,
                FirstName = FirstName,
                LastName = LastName,
                BirthDate = BirthDate,
                Gender = Gender,
                MaritalStatus = MaritalStatus,
                DiType = DiType,
                DiNumber = DiNumber,
                DiEmissionCountry = DiEmissionCountry,
                DiEmitionDate = DiEmitionDate,
                DiExpiryDate = DiExpiryDate,
                DiFrontalImage = DiFrontalImage,
                DiFrontalImageFile = DiFrontalImageFile,
                DiBackImage = DiBackImage,
                DiBackImageFile = DiBackImageFile,
                Country = Country,
                Province = Province,
                Municipio = Municipio,
                Address = Address,
                ResidenceProvince = ResidenceProvince,
                BayqiTag = BayqiTag,
                Password = Password,
                Pin = Pin,
                Profile = Profile,
                ProfileFile = ProfileFile,
                FathersName = FathersName,
                MothersName = MothersName,
                LegalRepresentativeType = LegalRepresentativeType,
                LegalRepresentativeName = LegalRepresentativeName,
                LegalRepresentativePhoneNumber = LegalRepresentativePhoneNumber
            };
        }

    }
}
