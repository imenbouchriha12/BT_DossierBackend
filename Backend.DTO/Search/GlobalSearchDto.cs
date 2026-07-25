using Backend.DTO.Client;

namespace Backend.DTO.Search
{
    public class GlobalSearchResultDto
    {
        public List<ClientSearchItemDto> Clients { get; set; } = new();
        public List<DocumentSearchItemDto> Documents { get; set; } = new();
        public int TotalCount => Clients.Count + Documents.Count;
    }

    public class ClientSearchItemDto
    {
        public int Id { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Ville { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
    }

    public class DocumentSearchItemDto
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public string TypeDocument { get; set; } = string.Empty;
        public string NomFichier { get; set; } = string.Empty;
        public string StatutValidation { get; set; } = string.Empty;
    }
}
