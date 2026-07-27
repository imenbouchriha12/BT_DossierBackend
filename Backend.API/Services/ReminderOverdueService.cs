using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Notification;

namespace Backend.API.Services
{
    public class ReminderOverdueService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderOverdueService> _logger;

        public ReminderOverdueService(IServiceProvider serviceProvider, ILogger<ReminderOverdueService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReminderOverdueService démarré");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckOverdueRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la vérification des rappels en retard");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task CheckOverdueRemindersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IReminderRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var overdueReminders = await repository.GetOverdueUnnotifiedAsync();

            foreach (var reminder in overdueReminders)
            {
                var targetUserId = reminder.AssignedToUserId ?? reminder.CreatedByUserId;
                var targetUser = reminder.AssignedToUser ?? reminder.CreatedByUser;

                if (targetUser == null) continue;

                var clientInfo = reminder.Client != null ? $" pour le client {reminder.Client.Prenom} {reminder.Client.Nom}" : "";
                var priorityEmoji = reminder.Priority switch
                {
                    "Critique" => "[URGENT] ",
                    "Haute" => "[HAUTE] ",
                    _ => ""
                };

                // 1. Notification in-app
                await notificationService.CreateAsync(new CreateNotificationDto
                {
                    UserId = targetUserId,
                    Titre = $"{priorityEmoji}Rappel en retard : {reminder.Title}",
                    Message = $"Le rappel \"{reminder.Title}\" était prévu le {reminder.DueDate:dd/MM/yyyy à HH:mm}{clientInfo}. Veuillez le traiter dès que possible.",
                    Type = reminder.Priority == "Critique" ? "Urgent" : "Alerte",
                    Lien = "/agenda"
                });

                // 2. Email
                var emailSubject = $"{priorityEmoji}Rappel en retard - {reminder.Title}";
                var emailBody = BuildOverdueEmailBody(
                    targetUser.Username,
                    reminder.Title,
                    reminder.Description,
                    reminder.DueDate,
                    reminder.Priority,
                    clientInfo);

                await emailService.SendDirectEmailAsync(targetUser.Email, targetUser.Username, emailSubject, emailBody);

                // Marquer comme notifié
                reminder.NotifiedAt = DateTime.UtcNow;
                await repository.UpdateAsync(reminder);
                await repository.SaveChangesAsync();

                _logger.LogInformation("Notification overdue envoyée pour le rappel {Id}: {Title} -> {User}", reminder.Id, reminder.Title, targetUser.Username);
            }

            if (overdueReminders.Count > 0)
            {
                _logger.LogInformation("{Count} rappels en retard notifiés", overdueReminders.Count);
            }
        }

        private static string BuildOverdueEmailBody(string userName, string title, string? description, DateTime dueDate, string priority, string clientInfo)
        {
            var priorityColor = priority switch
            {
                "Critique" => "#dc2626",
                "Haute" => "#ea580c",
                _ => "#d97706"
            };

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f7fb;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f7fb; padding: 40px 0;'>
    <tr><td align='center'>
      <table width='560' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 16px; box-shadow: 0 4px 20px rgba(0,0,0,0.06); overflow: hidden;'>
        <tr>
          <td style='background: linear-gradient(135deg, #1a237e 0%, #4a148c 100%); padding: 32px; text-align: center;'>
            <div style='width: 56px; height: 56px; background: rgba(255,0,0,0.2); border-radius: 14px; display: inline-flex; align-items: center; justify-content: center; margin-bottom: 16px;'>
              <span style='font-size: 28px;'>&#9200;</span>
            </div>
            <h1 style='color: #ffffff; font-size: 22px; font-weight: 600; margin: 0;'>Rappel en retard</h1>
          </td>
        </tr>
        <tr>
          <td style='padding: 32px;'>
            <p style='color: #1e293b; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>Bonjour <strong>{userName}</strong>,</p>
            <p style='color: #475569; font-size: 15px; line-height: 1.6; margin: 0 0 20px 0;'>
              Un rappel est maintenant <strong style='color: {priorityColor}'>en retard</strong> et nécessite votre attention.
            </p>
            <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #fef2f2; border: 1px solid #fecaca; border-radius: 12px; margin-bottom: 24px;'>
              <tr>
                <td style='padding: 20px;'>
                  <table width='100%' cellpadding='0' cellspacing='0'>
                    <tr>
                      <td style='padding: 4px 0;'>
                        <span style='color: #64748b; font-size: 13px;'>Titre</span><br/>
                        <span style='color: #1e293b; font-size: 15px; font-weight: 600;'>{title}</span>
                      </td>
                    </tr>
                    <tr>
                      <td style='padding: 4px 0;'>
                        <span style='color: #64748b; font-size: 13px;'>Date prévue</span><br/>
                        <span style='color: #dc2626; font-size: 15px; font-weight: 600;'>{dueDate:dd/MM/yyyy à HH:mm}</span>
                      </td>
                    </tr>
                    <tr>
                      <td style='padding: 4px 0;'>
                        <span style='color: #64748b; font-size: 13px;'>Priorité</span><br/>
                        <span style='color: {priorityColor}; font-size: 15px; font-weight: 600;'>{priority}</span>
                      </td>
                    </tr>
                    {(string.IsNullOrEmpty(clientInfo) ? "" : $@"<tr>
                      <td style='padding: 4px 0;'>
                        <span style='color: #64748b; font-size: 13px;'>Client</span><br/>
                        <span style='color: #1e293b; font-size: 15px; font-weight: 600;'>{clientInfo.Replace(" pour le client ", "")}</span>
                      </td>
                    </tr>")}
                    {(string.IsNullOrEmpty(description) ? "" : $@"<tr>
                      <td style='padding: 4px 0;'>
                        <span style='color: #64748b; font-size: 13px;'>Description</span><br/>
                        <span style='color: #475569; font-size: 14px;'>{description}</span>
                      </td>
                    </tr>")}
                  </table>
                </td>
              </tr>
            </table>
            <p style='color: #475569; font-size: 14px; line-height: 1.5; margin: 0 0 24px 0;'>
              Veuillez traiter ce rappel dès que possible ou le mettre à jour dans l'application.
            </p>
            <p style='color: #94a3b8; font-size: 13px; line-height: 1.5; margin: 0; text-align: center;'>
              Cet email a été généré automatiquement. Merci de ne pas y répondre.<br/>
              &copy; 2026 GestionBanque. Tous droits réservés.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
        }
    }
}
