using MailKit.Net.Smtp;
using MimeKit;

namespace AuthAPI.Services
{
    public interface IEmailOtpService
    {
        Task SendWelcomeEmailAsync(string toEmail, string IUF, string userName);
    }

    public class EmailOtpService : IEmailOtpService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public EmailOtpService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string IUF, string userName)
        {
            var subject = "Bem-vindo(a) à BayQi!";
            var htmlBody = GenerateWelcomeEmailHtml(userName, IUF);

            // Cria a versão texto simples
            var textBody = $@"Olá, {userName}!

                Estamos muito felizes em tê-lo(a) a bordo.  A sua conta foi criada com sucesso e já está pronta a ser utilizada.
                
                O seu Nº de Conta BayQi é:
                {IUF}
                ";

            // Reaproveita o método já existente para envio
            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("BayQi", "no-replay@bayqi.com"));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            // Extrair o código de verificação do HTML para a versão texto
            var codeMatch = System.Text.RegularExpressions.Regex.Match(htmlBody, @"<div class='verification-code'>(.*?)</div>");
            var verificationCode = codeMatch.Success ? codeMatch.Groups[1].Value : "N/A";

            // Extrair o nome do usuário do HTML para a versão texto
            var nameMatch = System.Text.RegularExpressions.Regex.Match(htmlBody, @"Olá (.*?),");
            var userName = nameMatch.Success ? nameMatch.Groups[1].Value : "Utilizador";

            var textBody = $@"Código De Verificação BayQi

Olá {userName},

Recebemos um pedido para verificar a sua conta BayQi.
O seu código de verificação é: {verificationCode}

Importante: Nunca partilhe este código com ninguém. A BayQi nunca solicitará o seu código de verificação por telefone ou email.

Obrigado,
BayQi

---
A BayQi tem o compromisso de prevenir e-mails fraudulentos.
Os e-mails enviados pelo BayQi irão sempre tratá-lo(a) pelo seu primeiro nome e apelido.
Não responda a este e-mail. Para entrar em contacto connosco, clique em Ajuda e Contactos.";

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = htmlBody;
            bodyBuilder.TextBody = textBody;

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync("mail.bayqi.com", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync("no-replay@bayqi.com", "3wG~{glVrLIgU$W$");
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Erro ao enviar e-mail: " + ex.Message);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }


        private string GenerateWelcomeEmailHtml(string userName, string accountNumber)
        {
            return $@"
<!DOCTYPE html>
<html lang='pt'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Conta criada com sucesso - BayQi</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 0;
            background-color: #ffffff;
            line-height: 1.6;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background-color: #ffffff;
            padding: 30px 20px;
            text-align: center;
            border-bottom: 1px solid #e9ecef;
        }}
        .logo {{
            max-width: 200px;
            height: auto;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .greeting {{
            font-size: 20px;
            color: #333;
            margin-bottom: 20px;
        }}
        .message {{
            font-size: 16px;
            color: #555;
            margin-bottom: 30px;
        }}
        .account-number {{
            background: linear-gradient(135deg, #f59e0b 0%, #fbbf24 100%);
            color: white;
            font-size: 22px;
            text-align: center;
            font-weight: bold;
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
            box-shadow: 0 4px 8px rgba(245, 158, 11, 0.3);
            letter-spacing: 2px;
        }}
        ul {{
            list-style: none;
            padding: 0;
            margin: 0 0 30px 0;
        }}
        ul li {{
            font-size: 16px;
            color: #555;
            margin-bottom: 10px;
            padding-left: 20px;
            position: relative;
        }}
        ul li::before {{
            content: '•';
            color: #f59e0b;
            position: absolute;
            left: 0;
        }}
        .footer {{
            background-color: #ffffff;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e9ecef;
        }}
        .footer-links a {{
            color: #6c757d;
            text-decoration: none;
            margin: 0 10px;
            font-size: 14px;
        }}
        .footer-links a:hover {{
            color: #3b82f6;
        }}
        .signature {{
            color: #6c757d;
            font-size: 14px;
            margin-top: 20px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <img src='https://fvtobubdapbxnwaplnrp.supabase.co/storage/v1/object/public/general/01logo.jpeg' alt='BayQi' class='logo' />
        </div>

        <div class='content'>
            <div class='greeting'>Olá, {userName}!</div>

            <div class='message'>
                Estamos muito felizes em tê-lo(a) a bordo. A sua conta foi criada com sucesso e já está pronta a ser utilizada.
                <br><br>
                O seu <strong>Nº de Conta BayQi</strong> é:
            </div>

            <div class='account-number'>{accountNumber}</div>

            <div class='message'>Com a BayQi pode:</div>

            <ul>
                <li>Enviar e receber dinheiro em segundos</li>
                <li>Pagar serviços e compras de forma simples e segura</li>
                <li>Carregar saldo e gerir o seu dinheiro a qualquer momento</li>
            </ul>

            <div class='message'>
                Se tiver alguma dúvida, entre em contacto com a nossa equipa pelo chat do aplicativo.
            </div>

            <div class='signature'>
                — Equipa BayQi
            </div>
        </div>

        <div class='footer'>
            <div class='footer-links'>
                <a href='https://bayqi.com'>Ajuda e Contactos</a>
                <a href='https://bayqi.com'>Segurança</a>
                <a href='https://bayqi.com'>Aplicações</a>
            </div>
            <p style='font-size:13px; color:#888;'>Este é um e-mail automático. Por favor, não responda.</p>
        </div>
    </div>
</body>
</html>";
        }


    }
}
