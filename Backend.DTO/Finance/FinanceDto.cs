namespace Backend.DTO.Finance
{
    public class CompteDto
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

    public class MouvementDto
    {
        public int Id { get; set; }
        public string NumCompte { get; set; } = string.Empty;
        public string Agence { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public DateTime DateMvt { get; set; }
        public string DevMVT { get; set; } = string.Empty;
        public string Sens { get; set; } = string.Empty;
    }

    public class CoursDto
    {
        public int Id { get; set; }
        public DateTime Journee { get; set; }
        public string Devise { get; set; } = string.Empty;
        public decimal Valeur { get; set; }
        public int Multiplicateur { get; set; }
    }

    public class CompteTotalDto
    {
        public string Devise { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal TotalContreValeur { get; set; }
        public int NombreComptes { get; set; }
    }

    /// <summary>
    /// Cours de change en temps réel : valeur d'une unité de devise en TND.
    /// </summary>
    public class CoursTempsReelDto
    {
        public string Devise { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Symbole { get; set; } = string.Empty;
        public decimal Valeur { get; set; }
        public DateTime? Date { get; set; }
    }
}
