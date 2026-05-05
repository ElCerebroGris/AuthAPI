using AuthAPI.Entities;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Services
{
    public interface IPhoneOtpService {
        Task SendOtpAsync(string phoneNumber);
        Task<bool> VerifyOtpAsync(string phoneNumber, string otp);
        Task SendWelcomeSmsAsync(string phoneNumber, string IUF, string userName);
        Task SendCustomSmsAsync(string phoneNumber, string message);
    }

    public class PhoneOtpService : IPhoneOtpService
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string APIToken = "70a56857-2154-4e63-a12e-3c02653a8247";//"JVQNAHNKNN56UMKV";
        private const string Url2 = "https://api.useombala.ao/v1/messages";

        public PhoneOtpService(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendOtpAsync(string phoneNumber)
        {
            var otp = new Random().Next(100000, 999999).ToString();

            var payload = new
            {
                message = $"Seu codigo de verificacao para BayQi: {otp}. Nunca compartilhe este codigo com ninguem.",
                to = phoneNumber,
                from = "BayQi"
            };

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", APIToken);

            var request = new HttpRequestMessage(HttpMethod.Post, Url2)
            {
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                )
            };

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Erro ao enviar OTP.");

            var existing = await _context.PhoneOtps.FirstOrDefaultAsync(p => p.PhoneNumber.Equals(phoneNumber));

            if (existing != null)
                _context.PhoneOtps.Remove(existing);

            _context.PhoneOtps.Add(new PhoneOtp
            {
                PhoneNumber = phoneNumber,
                Code = otp,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task<bool> VerifyOtpAsync(string phoneNumber, string otp)
        {
            var entry = await _context.PhoneOtps.FirstOrDefaultAsync(p => p.PhoneNumber.Equals(phoneNumber));
            if (entry == null || entry.Code != otp || (DateTime.UtcNow - entry.CreatedAt).TotalMinutes > 10)
                return false;

            _context.PhoneOtps.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SendWelcomeSmsAsync(string phoneNumber, string IUF, string userName)
        {
            var message = $"Ola {userName}, a sua conta BayQi foi criada com sucesso. " +
                $"No da conta: {IUF}. Agora pode enviar e receber dinheiro, pagar servicos e muito mais.";

            string phoneNumber9 = phoneNumber.Length >= 9 ?
                phoneNumber.Substring(phoneNumber.Length - 9) : phoneNumber;

            var payload = new
            {
                Message = message,
                To = phoneNumber9,
                From = "BayQi"
            };

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", APIToken);

            var response = await client.PostAsJsonAsync(Url2, payload);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Erro ao enviar SMS de boas-vindas.");
        }

        public async Task SendCustomSmsAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Número de telefone inválido.");

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Mensagem inválida.");

            string targetPhoneNumber = phoneNumber.Length >= 9
                ? phoneNumber.Substring(phoneNumber.Length - 9)
                : phoneNumber;

            var payload = new
            {
                Message = message,
                To = targetPhoneNumber,
                From = "BayQi"
            };

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", APIToken);

            var response = await client.PostAsJsonAsync(Url2, payload);

            if (response.IsSuccessStatusCode)
                return;

            var fallbackPayload = new
            {
                APIToken,
                Destino = new[] { targetPhoneNumber },
                Mensagem = message,
                CEspeciais = "false"
            };

            var fallbackClient = _httpClientFactory.CreateClient();
            fallbackClient.Timeout = TimeSpan.FromSeconds(10);
            var fallbackResponse = await fallbackClient.PostAsJsonAsync(Url2, fallbackPayload);

            if (!fallbackResponse.IsSuccessStatusCode)
                throw new Exception("Erro ao enviar SMS personalizado.");
        }
    }
}
