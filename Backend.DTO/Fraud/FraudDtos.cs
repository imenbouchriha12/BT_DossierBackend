namespace Backend.DTO.Fraud
{
    /// <summary>Règle de détection de fraude (configurable)</summary>
    public class RegleAntifraudeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Seuil { get; set; }
        public decimal? SeuilSecondaire { get; set; }
        public int PeriodeJours { get; set; } = 90;
        public int Poids { get; set; } = 10;
        public bool Actif { get; set; } = true;
        public int Priorite { get; set; } = 0;
        public DateTime DateCreation { get; set; }
    }

    public class CreateRegleAntifraudeDto
    {
        public string Code { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = "MontantMaximal";
        public decimal Seuil { get; set; }
        public decimal? SeuilSecondaire { get; set; }
        public int PeriodeJours { get; set; } = 90;
        public int Poids { get; set; } = 10;
        public bool Actif { get; set; } = true;
        public int Priorite { get; set; } = 0;
    }

    public class UpdateRegleAntifraudeDto
    {
        public string? Code { get; set; }
        public string? Nom { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public decimal? Seuil { get; set; }
        public decimal? SeuilSecondaire { get; set; }
        public int? PeriodeJours { get; set; }
        public int? Poids { get; set; }
        public bool? Actif { get; set; }
        public int? Priorite { get; set; }
    }

    /// <summary>Alerte de fraude / anomalie</summary>
    public class AlertesFraudeDto
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public int? IdRegle { get; set; }
        public string? NomRegle { get; set; }
        public int? IdMouvement { get; set; }
        public string? NumCompte { get; set; }
        public string? Agence { get; set; }
        public decimal? Montant { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Niveau { get; set; } = "Moyenne";
        public int Score { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Statut { get; set; } = "Nouvelle";
        public string? DonneesJson { get; set; }
        public DateTime DateDetection { get; set; }
        public int? TraiteeParUserId { get; set; }
        public DateTime? DateTraitement { get; set; }
        public string? Commentaire { get; set; }
    }

    public class AlertesFraudeSearchDto
    {
        public int? IdClient { get; set; }
        public string? Statut { get; set; }
        public string? Niveau { get; set; }
        public string? Type { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class TraiterAlerteFraudeDto
    {
        public string Statut { get; set; } = "Traitee";
        public string? Commentaire { get; set; }
    }

    /// <summary>Résultat d'une analyse de détection</summary>
    public class AnalyseFraudeResultatDto
    {
        public int IdClient { get; set; }
        public int AlertesCrees { get; set; }
        public int AlertesExistantes { get; set; }
        public List<AlerteDetecteeDto> Alertes { get; set; } = new();
    }

    public class AlerteDetecteeDto
    {
        public string Type { get; set; } = string.Empty;
        public string Niveau { get; set; } = "Moyenne";
        public int Score { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? NumCompte { get; set; }
        public decimal? Montant { get; set; }
    }

    public class FraudStatsDto
    {
        public int TotalAlertes { get; set; }
        public int AlertesActives { get; set; }
        public int Critique { get; set; }
        public int Haute { get; set; }
        public int Moyenne { get; set; }
        public int Traitees { get; set; }
    }
}
