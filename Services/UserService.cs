using AuthAPI.DTOs;
using AuthAPI.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace AuthAPI.Services
{
    public interface IUserService
    {
        Task<RegisterCustomerUserResponse> RegisterCustomerUserAsync(RegisterCustomerUserRequest dto);

        Task<RegisterCustomerUserResponse> LoginCustomerUserAsync(string phoneNumber, string password);

        Task<RegisterCustomerUserResponse> LoginCustomerUserByBiAsync(string biNumber, string password);

        Task<object> RegisterCustomerUserByBiAsync(RegisterCustomerByBIRequest dto);
    }

    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPhoneOtpService _phoneOtpService;
        private readonly IEmailOtpService _emailOtpService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UserService> _logger;
        private readonly string CALLBACK_URL = "https://api.bayqi.ao/api/wallet-payment/reference_wook_payment";

        public UserService(AppDbContext context, UserManager<User> userManager, IConfiguration configuration,
            IPhoneOtpService phoneOtpService, ILogger<UserService> logger,
            IEmailOtpService emailOtpService, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _phoneOtpService = phoneOtpService;
            _emailOtpService = emailOtpService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<RegisterCustomerUserResponse> RegisterCustomerUserAsync(RegisterCustomerUserRequest dto)
        {
            //var existingUser = await _userManager.Users
            //    .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber || u.Email == dto.Email);

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber) && string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new InvalidOperationException("Telefone/Email inválido.");
            }

            if (await CustomerExistsAsync(dto.DiNumber))
            {
                throw new InvalidOperationException("Documento de identificação inválido.");
            }

            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                query = query.Where(u => u.PhoneNumber == dto.PhoneNumber);
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                query = query.Where(u => u.Email == dto.Email);
            }

            var existingUser = await query.FirstOrDefaultAsync();

            if (existingUser != null && await _userManager.IsInRoleAsync(existingUser, "Customer"))
                throw new InvalidOperationException("Telefone/Email inválido.");

            if (existingUser == null)
            {
                var lastOrder = await _context.Users.CountAsync();

                var newUser = new User
                {
                    Order = lastOrder + 1,
                    UserName = Guid.NewGuid().ToString(),
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    IUF = GenerateSimpleIUF("1", "0", dto.DiNumber, dto.FirstName, dto.BirthDate.Year),
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(newUser, dto.Password);

                if (!result.Succeeded)
                    throw new InvalidOperationException("Erro na criação da conta: " + string.Join(", ", result.Errors.Select(e => e.Description)));

                await _userManager.AddToRoleAsync(newUser, "Customer");
                existingUser = newUser;
            }

            var age = DateTime.Today.Year - dto.BirthDate.Year;
            if (dto.BirthDate.Date > DateTime.Today.AddYears(-age)) age--;

            var isMinor = age < 18;

            var customer = new Customer
            {
                UserId = existingUser.Id,
                FirstName = dto.FirstName,
                SurName = dto.LastName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Nationality = dto.Nationality,
                Dob = dto.BirthDate.ToString("yyyy-MM-dd"),
                Gender = dto.Gender,
                MaritalStatus = dto.MaritalStatus,
                IdentificationType = dto.DiType,
                IdentificationNumber = dto.DiNumber,
                IdentificationEmissionCountry = dto.DiEmissionCountry,
                IdentificationExpiryDate = dto.DiExpiryDate.ToString("yyyy-MM-dd"),
                IdentificationEmissionDate = dto.DiEmitionDate?.ToString("yyyy-MM-dd"),
                DiFrontalImage = dto.DiFrontalImage,
                DiBackImage = dto.DiBackImage,
                Province = NormalizeProvinceName(dto.Province),
                Municipio = NormalizeMunicipalityName(dto.Municipio),
                Country = dto.Country,
                BillingAddress = dto.Address,
                ShippingAddress = dto.Address,
                BayqiTag = dto.BayqiTag,
                Pin = dto.Pin,
                WalletBalance = 0,
                Profile = dto.Profile,
                IsActive = true,
                IsDocumentAccount = false,

                FathersName = isMinor ? dto.FathersName : null,
                MothersName = isMinor ? dto.MothersName : null,
                LegalRepresentativeType = isMinor ? dto.LegalRepresentativeType : null,
                LegalRepresentativeName = isMinor ? dto.LegalRepresentativeName : null,
                LegalRepresentativePhoneNumber = isMinor ? dto.LegalRepresentativePhoneNumber : null,
                MongoCreatedAt = DateTime.UtcNow.ToLongDateString() 
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var roles = await _userManager.GetRolesAsync(existingUser);

            await GenerateReferencePaymentAsync(existingUser.IUF!);

            if (!string.IsNullOrEmpty(customer.Email))
                await _emailOtpService.SendWelcomeEmailAsync(customer.Email, existingUser.IUF!, customer.FirstName + " " + customer.SurName);
            else if (!string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await _phoneOtpService.SendWelcomeSmsAsync(customer.PhoneNumber, existingUser.IUF!, customer.FirstName + " " + customer.SurName);
            }

            return await LoginCustomerUserAsync(dto.PhoneNumber ?? dto.Email, dto.Password);
        }

        public async Task<RegisterCustomerUserResponse> LoginCustomerUserAsync(string phoneNumber, string password)
        {
            // Buscar todos os usuários com o mesmo PhoneNumber
            var users = await _userManager.Users
                .Where(u => (!string.IsNullOrEmpty(u.PhoneNumber) &&
                            u.PhoneNumber.EndsWith(phoneNumber))
                            || u.Email == phoneNumber).ToListAsync();

            // Encontrar o primeiro usuário que tem a role "Seller"
            User? customerUser = null;
            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Customer"))
                {
                    customerUser = user;
                    break; // Para no primeiro usuário encontrado
                }
            }

            if (customerUser == null)
                throw new UnauthorizedAccessException("Credenciais inválidas");

            //var result = await _signInManager.PasswordSignInAsync(sellerUser, password, false, false);

            var isPasswordValid = await _userManager.CheckPasswordAsync(customerUser, password);

            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Credenciais inválidas");

            if (!customerUser.IsActive)
            {
                customerUser.IsActive = true;
                _context.Users.Update(customerUser);

                var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == customerUser.Id);

                if (existingCustomer != null)
                {
                    existingCustomer.IsActive = true;
                    _context.Customers.Update(existingCustomer);
                }

                await _context.SaveChangesAsync();
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == customerUser.Id);

            return new RegisterCustomerUserResponse
            {
                Id = customerUser.Id,
                Order = customerUser.Order,
                IUF = customerUser.IUF,
                BotpressKey = customerUser.BotpressKey,
                Token = GenerateJwtToken(customerUser),
                Experies_in = 3600,
                IsActive = customerUser.IsActive
            };
        }

        public async Task<RegisterCustomerUserResponse> LoginCustomerUserByBiAsync(string biNumber, string password)
        {
            if (string.IsNullOrWhiteSpace(biNumber))
                throw new UnauthorizedAccessException("Credenciais inválidas");

            var normalizedBi = biNumber.Trim();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => !string.IsNullOrEmpty(c.IdentificationNumber) &&
                    c.IdentificationNumber.Trim().ToLower() == normalizedBi.ToLower());

            if (customer == null || string.IsNullOrWhiteSpace(customer.UserId))
                throw new UnauthorizedAccessException("Credenciais inválidas");

            var customerUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == customer.UserId);

            if (customerUser == null)
                throw new UnauthorizedAccessException("Credenciais inválidas");

            if (!await _userManager.IsInRoleAsync(customerUser, "Customer"))
                throw new UnauthorizedAccessException("Credenciais inválidas");

            var isPasswordValid = await _userManager.CheckPasswordAsync(customerUser, password);

            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Credenciais inválidas");

            if (!customerUser.IsActive)
            {
                customerUser.IsActive = true;
                _context.Users.Update(customerUser);

                customer.IsActive = true;
                _context.Customers.Update(customer);

                await _context.SaveChangesAsync();
            }

            return new RegisterCustomerUserResponse
            {
                Id = customerUser.Id,
                Order = customerUser.Order,
                IUF = customerUser.IUF,
                BotpressKey = customerUser.BotpressKey,
                Token = GenerateJwtToken(customerUser),
                Experies_in = 3600,
                IsActive = customerUser.IsActive
            };
        }

        public async Task<object> RegisterCustomerUserByBiAsync(RegisterCustomerByBIRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.DiNumber))
                throw new InvalidOperationException("Documento de identificação inválido.");

            if (await CustomerExistsAsync(dto.DiNumber))
                throw new InvalidOperationException("Documento de identificação inválido.");

            dto.PhoneNumber = null;
            dto.Email = null;

            var lastOrder = await _context.Customers
                .Join(_context.Users,
                    c => c.UserId, u => u.Id,
                    (c, u) => (int?)u.Order)
                .MaxAsync() ?? 0;

            var newUser = new User
            {
                Order = lastOrder + 1,
                UserName = Guid.NewGuid().ToString(),
                PhoneNumber = null,
                Email = null,
                IUF = GenerateSimpleIUF("1", "0", dto.DiNumber, dto.FirstName, dto.BirthDate.Year),
                IsActive = true
            };

            var result = await _userManager.CreateAsync(newUser, dto.Password);

            if (!result.Succeeded)
                throw new InvalidOperationException("Erro na criação da conta: " + string.Join(", ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(newUser, "Customer");

            var age = DateTime.Today.Year - dto.BirthDate.Year;
            if (dto.BirthDate.Date > DateTime.Today.AddYears(-age)) age--;

            var isMinor = age < 18;

            var lastCustomerNumber = await _context.Customers
                .MaxAsync(c => (int?)c.Number) ?? 0;

            var customer = new Customer
            {
                UserId = newUser.Id,
                Number = lastCustomerNumber + 1,
                FirstName = dto.FirstName,
                SurName = dto.LastName,
                Email = null,
                PhoneNumber = null,
                Nationality = dto.Nationality,
                Dob = dto.BirthDate.ToString("yyyy-MM-dd"),
                Gender = dto.Gender,
                MaritalStatus = dto.MaritalStatus,
                IdentificationType = dto.DiType,
                IdentificationNumber = dto.DiNumber,
                IdentificationEmissionCountry = dto.DiEmissionCountry,
                IdentificationExpiryDate = dto.DiExpiryDate.ToString("yyyy-MM-dd"),
                IdentificationEmissionDate = dto.DiEmitionDate?.ToString("yyyy-MM-dd"),
                DiFrontalImage = dto.DiFrontalImage,
                DiBackImage = dto.DiBackImage,
                Province = dto.Province,
                Municipio = dto.Municipio,
                Country = dto.Country,
                ResidenceProvince = dto.ResidenceProvince,
                BillingAddress = dto.Address,
                ShippingAddress = dto.Address,
                BayqiTag = dto.BayqiTag,
                Pin = dto.Pin,
                WalletBalance = 0,
                Profile = dto.Profile,
                IsActive = true,

                FathersName = isMinor ? dto.FathersName : null,
                MothersName = isMinor ? dto.MothersName : null,
                LegalRepresentativeType = isMinor ? dto.LegalRepresentativeType : null,
                LegalRepresentativeName = isMinor ? dto.LegalRepresentativeName : null,
                LegalRepresentativePhoneNumber = isMinor ? dto.LegalRepresentativePhoneNumber : null,
                MongoCreatedAt = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                IsDocumentAccount = true
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await GenerateReferencePaymentAsync(newUser.IUF!);

            // Registo por BI não dispara comunicação automática.
            return await LoginCustomerUserByBiAsync(dto.DiNumber, dto.Password);
        }

        private async Task<bool> CustomerExistsAsync(string documentID)
        {
            return await _context.Customers.AsNoTracking()
                .AnyAsync(c => c.IdentificationNumber.ToLower().Equals(documentID.ToLower()));
        }

        private static string GenerateSimpleIUF(string tipo, string zonaCodigo, string nif, string nome, int ano)
        {
            tipo = tipo.PadLeft(1, '0'); // Pessoa: 1
            zonaCodigo = zonaCodigo.PadLeft(1, '0'); // Ex: "2" → "2"
            var anoCode = ano.ToString().Substring(2); // 2025 → "25"
            var nifSuffix = nif.Length >= 2 ? nif[^2..] : nif.PadLeft(2, '0');

            // Gerar hash simples baseado no nome e NIF
            string baseString = nome + nif;
            int hash = Math.Abs(baseString.GetHashCode());
            string hashPart = (hash % 1000).ToString("D3");

            return $"{tipo}{zonaCodigo}{nifSuffix}{anoCode}{hashPart}";
        }

        private static string? NormalizeMunicipalityName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var normalized = Regex.Replace(value.Trim(), @"\s+", " ");
            normalized = Regex.Replace(normalized, @"\s+(municipio|município|city)$", string.Empty, RegexOptions.IgnoreCase);
            return ToTitleCasePt(normalized);
        }

        private static string? NormalizeProvinceName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var normalized = Regex.Replace(value.Trim(), @"\s+", " ");
            normalized = Regex.Replace(normalized, @"\s+(province|provincia|província)$", string.Empty, RegexOptions.IgnoreCase);
            return ToTitleCasePt(normalized);
        }

        private static string ToTitleCasePt(string value)
        {
            var textInfo = CultureInfo.GetCultureInfo("pt-PT").TextInfo;
            return textInfo.ToTitleCase(value.ToLowerInvariant());
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id), // 🔥 Alternativamente, tente JwtRegisteredClaimNames.Sub
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id) // 🔥 Adiciona a claim correta para IdentityConstants.BearerScheme
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task GenerateReferencePaymentAsync(string iuf)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(130);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "96fmdoi3r09ujhsci51i803puhj1t1rb");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.proxypay.v2+json"));

            try
            {
                var referencePayload = new
                {
                    custom_fields = new
                    {
                        callback_url = CALLBACK_URL
                    }
                };

                var referenceContent = new StringContent(JsonConvert.SerializeObject(referencePayload), Encoding.UTF8, "application/json");

                var putResponse = await httpClient.PutAsync($"https://api.proxypay.co.ao/references/{iuf}", referenceContent);

                if (!putResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Erro ao definir referência ProxyPay: {StatusCode} {Content}", putResponse.StatusCode, await putResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    _logger.LogInformation("Referencia de pagamento criada com sucesso: {ReferenceId} como IUF", iuf);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao gerar pagamento por referência ProxyPay.", ex);
            }
        }
    }
}
