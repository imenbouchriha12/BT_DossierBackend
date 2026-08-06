namespace Backend.DTO.Message
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ExpediteurUserId { get; set; }
        public string? ExpediteurNom { get; set; }
        public int DestinataireUserId { get; set; }
        public string? DestinataireNom { get; set; }
        public string Contenu { get; set; } = string.Empty;
        public string? PieceJointeUrl { get; set; }
        public string? PieceJointeNom { get; set; }
        public string? PieceJointeType { get; set; }
        public DateTime DateEnvoi { get; set; }
        public bool Lu { get; set; }
        public DateTime? DateLecture { get; set; }
    }

    public class ConversationDto
    {
        public int AutreUserId { get; set; }
        public string? AutreNom { get; set; }
        public string? AutreMatricule { get; set; }
        public string? AutreRole { get; set; }
        public bool EnLigne { get; set; }
        public string? DernierMessage { get; set; }
        public DateTime? DerniereDate { get; set; }
        public int NonLu { get; set; }
    }

    public class ConversationPagedDto
    {
        public List<MessageDto> Items { get; set; } = new();
        public int Total { get; set; }
    }

    public class SendMessageDto
    {
        public int DestinataireUserId { get; set; }
        public string Contenu { get; set; } = string.Empty;
        public string? PieceJointeUrl { get; set; }
        public string? PieceJointeNom { get; set; }
        public string? PieceJointeType { get; set; }
    }

    public class UploadPieceJointeDto
    {
        public string Url { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
