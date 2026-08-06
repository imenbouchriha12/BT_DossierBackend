namespace Backend.DTO.Risk
{
    /// <summary>Score de risque d'un client</summary>
    public class ScoreRisqueDto
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public int Score { get; set; }
        public string Niveau { get; set; } = "Faible";
        public string? ComposantsJson { get; set; }
        public string Synthese { get; set; } = string.Empty;
        public DateTime DateCalcul { get; set; }
    }

    /// <summary>Décomposition lisible du score de risque</summary>
    public class RapportRisqueDto
    {
        public int IdClient { get; set; }
        public int Score { get; set; }
        public string Niveau { get; set; } = "Faible";
        public string Synthese { get; set; } = string.Empty;
        public DateTime DateCalcul { get; set; }
        public List<ComposantRisqueDto> Composants { get; set; } = new();
    }

    public class ComposantRisqueDto
    {
        public string Code { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public int Points { get; set; }
        public int MaxPoints { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    public class HistoriqueRisqueDto
    {
        public List<ScoreRisqueDto> Items { get; set; } = new();
        public int Total { get; set; }
    }
}
