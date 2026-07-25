namespace Backend.DTO.Historique
{
    public class HistoriqueDto
    {
        public int Id { get; set; }
        public string TableModifiee { get; set; } = string.Empty;
        public int EntiteId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? AncienneValeur { get; set; }
        public string? NouvelleValeur { get; set; }
        public int UtilisateurId { get; set; }
        public string UtilisateurNom { get; set; } = string.Empty;
        public DateTime DateHeure { get; set; }
    }

    public class HistoriqueSearchDto
    {
        public string? TableModifiee { get; set; }
        public string? UtilisateurNom { get; set; }
        public string? Action { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PagedHistoriqueResult
    {
        public List<HistoriqueDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    }
}
