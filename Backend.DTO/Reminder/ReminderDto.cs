namespace Backend.DTO.Reminder
{
    public class ReminderDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientCode { get; set; }
        public int? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public int CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? NotifiedAt { get; set; }
    }

    public class CreateReminderDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = "Autre";
        public string Priority { get; set; } = "Normale";
        public DateTime DueDate { get; set; }
        public int? ClientId { get; set; }
        public int? AssignedToUserId { get; set; }
    }

    public class UpdateReminderDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public string? Priority { get; set; }
        public string? Status { get; set; }
        public DateTime? DueDate { get; set; }
        public int? AssignedToUserId { get; set; }
    }

    public class ReminderFilterDto
    {
        public string? Type { get; set; }
        public string? Priority { get; set; }
        public string? Status { get; set; }
        public int? ClientId { get; set; }
        public int? AssignedToUserId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    public class ReminderStatsDto
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Overdue { get; set; }
        public int Urgent { get; set; }
    }

    public class UpdateReminderStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class CreateDocumentReminderDto
    {
        public int ClientId { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
