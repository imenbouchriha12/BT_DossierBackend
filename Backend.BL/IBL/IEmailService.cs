namespace Backend.BL.IBL
{
    public interface IEmailService
    {
        Task SendActivationEmailAsync(string toEmail, string toName, string matricule, string password, string roleName);
        Task SendResetPasswordEmailAsync(string toEmail, string toName, string resetLink);
        Task SendDirectEmailAsync(string toEmail, string toName, string subject, string body);
    }
}

