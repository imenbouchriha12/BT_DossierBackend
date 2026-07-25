using System.ComponentModel.DataAnnotations;

namespace Backend.DTO.Auth
{
    public class SendEmailDto
    {
        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;
    }
}
