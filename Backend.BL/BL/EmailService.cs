using Backend.BL.IBL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendActivationEmailAsync(string toEmail, string toName, string matricule, string password, string roleName)
        {
            var subject = "Bienvenue sur BT DocCenter - Votre compte a été activé";
            var body = BuildActivationEmailBody(toName, matricule, password, roleName);

            var smtpHost = _configuration["Smtp:Host"];
            if (!string.IsNullOrWhiteSpace(smtpHost))
            {
                await SendViaSmtpAsync(toEmail, subject, body);
            }
            else
            {
                await SaveToFileAsync(toEmail, subject, body);
            }
        }

        public async Task SendResetPasswordEmailAsync(string toEmail, string toName, string resetLink)
        {
            var subject = "Réinitialisation de votre mot de passe - BT DocCenter";
            var body = BuildResetPasswordEmailBody(toName, resetLink);

            var smtpHost = _configuration["Smtp:Host"];
            if (!string.IsNullOrWhiteSpace(smtpHost))
            {
                await SendViaSmtpAsync(toEmail, subject, body);
            }
            else
            {
                await SaveToFileAsync(toEmail, subject, body);
            }
        }

        public async Task SendDirectEmailAsync(string toEmail, string toName, string subject, string body)
        {
            var smtpHost = _configuration["Smtp:Host"];
            if (!string.IsNullOrWhiteSpace(smtpHost))
            {
                await SendViaSmtpAsync(toEmail, subject, body);
            }
            else
            {
                await SaveToFileAsync(toEmail, subject, body);
            }
        }

        private string BuildResetPasswordEmailBody(string toName, string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f7fb;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f7fb; padding: 40px 0;'>
    <tr><td align='center'>
      <table width='560' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 16px; box-shadow: 0 4px 20px rgba(0,0,0,0.06); overflow: hidden;'>
        <tr>
          <td style='background: linear-gradient(135deg, #1a237e 0%, #4a148c 100%); padding: 40px 32px; text-align: center;'>
            <h1 style='color: #ffffff; font-size: 24px; font-weight: 600; margin: 0;'>Réinitialisation du mot de passe</h1>
          </td>
        </tr>
        <tr>
          <td style='padding: 40px 32px;'>
            <p style='color: #1e293b; font-size: 16px; line-height: 1.6; margin: 0 0 24px 0;'>Bonjour <strong>{toName}</strong>,</p>
            <p style='color: #475569; font-size: 15px; line-height: 1.6; margin: 0 0 24px 0;'>
              Vous avez demandé la réinitialisation de votre mot de passe. Cliquez sur le bouton ci-dessous pour en définir un nouveau.
            </p>
            <table width='100%' cellpadding='0' cellspacing='0'>
              <tr><td align='center' style='padding-bottom: 24px;'>
                <a href='{resetLink}' style='display: inline-block; background: linear-gradient(135deg, #1a237e 0%, #4a148c 100%); color: #ffffff; text-decoration: none; font-size: 15px; font-weight: 600; padding: 14px 36px; border-radius: 10px;'>Réinitialiser mon mot de passe</a>
              </td></tr>
            </table>
            <p style='color: #94a3b8; font-size: 13px; line-height: 1.5; margin: 0 0 24px 0; text-align: center;'>
              Ce lien expire dans 1 heure. Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.
            </p>
            <p style='color: #94a3b8; font-size: 13px; line-height: 1.5; margin: 0; text-align: center;'>
              &copy; 2024 BT DocCenter Solutions. Tous droits réservés.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
        }

        private string BuildActivationEmailBody(string toName, string matricule, string password, string roleName)
        {
            var appUrl = _configuration["AllowedOrigins"]?.Split(',')?.FirstOrDefault() ?? "http://localhost:4200";
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f7fb;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f7fb; padding: 40px 0;'>
    <tr><td align='center'>
      <table width='560' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 16px; box-shadow: 0 4px 20px rgba(0,0,0,0.06); overflow: hidden;'>
        <tr>
          <td style='background: linear-gradient(135deg, #1a237e 0%, #4a148c 100%); padding: 40px 32px; text-align: center;'>
            <div style='width: 56px; height: 56px; background: rgba(255,255,255,0.15); border-radius: 14px; display: inline-flex; align-items: center; justify-content: center; margin-bottom: 16px;'>
              <span style='font-size: 28px;'>&#9729;</span>
            </div>
            <h1 style='color: #ffffff; font-size: 24px; font-weight: 600; margin: 0;'>Compte activé avec succès</h1>
          </td>
        </tr>
        <tr>
          <td style='padding: 40px 32px;'>
            <p style='color: #1e293b; font-size: 16px; line-height: 1.6; margin: 0 0 24px 0;'>Bonjour <strong>{toName}</strong>,</p>
            <p style='color: #475569; font-size: 15px; line-height: 1.6; margin: 0 0 24px 0;'>
              Votre compte sur <strong>BT DocCenter</strong> a été activé par l'administrateur. Voici vos identifiants de connexion :
            </p>
            <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8fafc; border-radius: 12px; padding: 20px; margin-bottom: 24px;'>
              <tr>
                <td style='padding: 6px 0;'>
                  <span style='color: #64748b; font-size: 13px;'>Matricule</span><br/>
                  <span style='color: #1e293b; font-size: 16px; font-weight: 600;'>{matricule}</span>
                </td>
              </tr>
              <tr>
                <td style='padding: 6px 0;'>
                  <span style='color: #64748b; font-size: 13px;'>Mot de passe</span><br/>
                  <span style='color: #1e293b; font-size: 16px; font-weight: 600;'>{password}</span>
                </td>
              </tr>
              <tr>
                <td style='padding: 6px 0;'>
                  <span style='color: #64748b; font-size: 13px;'>Rôle attribué</span><br/>
                  <span style='color: #1e293b; font-size: 16px; font-weight: 600;'>{roleName}</span>
                </td>
              </tr>
            </table>
            <p style='color: #475569; font-size: 14px; line-height: 1.5; margin: 0 0 24px 0;'>
              Nous vous recommandons de changer votre mot de passe dès votre première connexion.
            </p>
            <table width='100%' cellpadding='0' cellspacing='0'>
              <tr><td align='center'>
                <a href='{appUrl}/connexion' style='display: inline-block; background: linear-gradient(135deg, #1a237e 0%, #4a148c 100%); color: #ffffff; text-decoration: none; font-size: 15px; font-weight: 600; padding: 14px 36px; border-radius: 10px;'>Accéder à mon espace</a>
              </td></tr>
            </table>
            <p style='color: #94a3b8; font-size: 13px; line-height: 1.5; margin: 32px 0 0 0; text-align: center;'>
              Cet email a été généré automatiquement. Merci de ne pas y répondre.<br/>
              &copy; 2024 BT DocCenter Solutions. Tous droits réservés.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
        }

        private async Task SendViaSmtpAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress(
                    _configuration["Smtp:FromName"] ?? "BT DocCenter",
                    _configuration["Smtp:FromEmail"] ?? "noreply@lumina-finance.com"));
                message.To.Add(new MimeKit.MailboxAddress("", toEmail));
                message.Subject = subject;
                message.Body = new MimeKit.TextPart("html") { Text = body };

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(_configuration["Smtp:Host"], int.Parse(_configuration["Smtp:Port"] ?? "587"), MailKit.Security.SecureSocketOptions.StartTls);
                var username = _configuration["Smtp:Username"];
                if (!string.IsNullOrWhiteSpace(username))
                {
                    await client.AuthenticateAsync(username, _configuration["Smtp:Password"] ?? "");
                }
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                _logger.LogInformation("Email envoyé à {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Échec d'envoi SMTP vers {Email}, sauvegarde fichier : {Message}", toEmail, ex.Message);
                await SaveToFileAsync(toEmail, subject, body);
            }
        }

        private async Task SaveToFileAsync(string toEmail, string subject, string body)
        {
            var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs", "emails");
            Directory.CreateDirectory(logsDir);
            var fileName = $"email_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.html";
            var filePath = Path.Combine(logsDir, fileName);
            var fullContent = $"<!-- To: {toEmail} | Subject: {subject} -->\n{body}";
            await File.WriteAllTextAsync(filePath, fullContent);
            _logger.LogInformation("Email sauvegardé dans {FilePath}", filePath);
        }
    }
}
