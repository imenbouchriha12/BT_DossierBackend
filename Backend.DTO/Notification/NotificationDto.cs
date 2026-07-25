namespace Backend.DTO.Notification
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Lien { get; set; }
        public bool EstLu { get; set; }
        public DateTime DateCreation { get; set; }
    }

    public class CreateNotificationDto
    {
        public int UserId { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info";
        public string? Lien { get; set; }
    }

    public class NotificationCountDto
    {
        public int TotalNonLu { get; set; }
    }
}
