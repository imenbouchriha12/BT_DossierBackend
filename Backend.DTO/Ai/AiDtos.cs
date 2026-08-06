namespace Backend.DTO.Ai
{
    /// <summary>Message d'une conversation avec l'assistant IA</summary>
    public class ChatMessageDto
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }

    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;

        /// Si renseigné, l'assistant répond dans le contexte du dossier client
        public int? ClientId { get; set; }
    }

    public class ChatResponseDto
    {
        public string Reponse { get; set; } = string.Empty;

        /// "llm" si l'IA a généré la réponse, "fallback" sinon
        public string Mode { get; set; } = "fallback";

        /// Données contextuelles utilisées pour construire la réponse
        public List<DonneeContextuelleDto>? Contexte { get; set; }
    }

    public class DonneeContextuelleDto
    {
        public string Type { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public string Valeur { get; set; } = string.Empty;
    }

    /// <summary>Résumé automatique d'un dossier client</summary>
    public class ClientSummaryDto
    {
        public int IdClient { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string? TypeClient { get; set; }
        public string Statut { get; set; } = string.Empty;
        public decimal SoldeTotal { get; set; }
        public int NbComptes { get; set; }
        public int NbDocumentsManquants { get; set; }
        public int NbAlertesActives { get; set; }
        public string? NiveauRisque { get; set; }
        public int? ScoreRisque { get; set; }

        /// Synthèse en 5 lignes maximum
        public string Resume { get; set; } = string.Empty;

        /// "llm" si généré par l'IA, "template" sinon
        public string Mode { get; set; } = "template";
    }
}
