namespace Backend.DTO.Client
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public string TypeDocument { get; set; } = string.Empty;
        public string NomFichier { get; set; } = string.Empty;
        public string CheminFichier { get; set; } = string.Empty;
        public DateTime DateAjout { get; set; }
        public string StatutValidation { get; set; } = string.Empty;
        public string? CodeClient { get; set; }
        public string? NomClient { get; set; }
    }

    public class UpdateDocumentValidationDto
    {
        public string StatutValidation { get; set; } = string.Empty;
    }
}
