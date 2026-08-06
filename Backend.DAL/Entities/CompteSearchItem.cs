namespace Backend.DAL.Entities
{
    /// Projection de lecture d'un compte avec les informations client associées
    /// (utilisée par la recherche globale de comptes).
    public class CompteSearchItem
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public string NumCompte { get; set; } = string.Empty;
        public string Agence { get; set; } = string.Empty;
        public string Devise { get; set; } = string.Empty;
        public decimal Solde { get; set; }
        public decimal SoldeContreValeur { get; set; }
        public DateTime? DateOuverture { get; set; }
        public string? CodeClient { get; set; }
        public string? NomClient { get; set; }
    }
}
