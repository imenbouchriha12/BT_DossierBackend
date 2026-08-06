using Backend.DTO.Finance;

namespace Backend.DTO.Dashboard
{
    /// <summary>
    /// Agrégats financiers du tableau de bord (données SISS : Comptes, MVT, Cours).
    /// </summary>
    public class FinanceDashboardDto
    {
        public FinanceKpisDto Kpis { get; set; } = new();
        public List<RepartitionDeviseDto> RepartitionParDevise { get; set; } = new();
        public List<EtatCompteDto> EtatsComptes { get; set; } = new();
        public List<EvolutionMouvementDto> EvolutionMouvements { get; set; } = new();
        public List<TopAgenceDto> TopAgences { get; set; } = new();
        public List<MouvementRecenteDto> DerniersMouvements { get; set; } = new();
        public List<CompteDecouvertDto> ComptesDecouverts { get; set; } = new();
        public List<CoursTempsReelDto> CoursJour { get; set; } = new();
    }

    public class FinanceKpisDto
    {
        /// Actif total consolidé en TND (somme des soldes contre-valeur)
        public decimal TotalActifsTND { get; set; }

        /// Nombre de comptes ouverts
        public int NbComptes { get; set; }

        /// Nombre de clients détenteurs d'au moins un compte
        public int NbClientsDetenteurs { get; set; }

        /// Nombre de comptes en découvert (solde &lt; 0)
        public int NbComptesDecouverts { get; set; }

        /// Taux de découvert en % (comptes en découvert / total)
        public double TauxDecouvert { get; set; }

        /// Nombre total de mouvements
        public int NbMouvements { get; set; }

        /// Volume total des crédits (entrées) en TND
        public decimal VolumeCreditsTND { get; set; }

        /// Volume total des débits (sorties) en TND
        public decimal VolumeDebitsTND { get; set; }

        /// Flux net = crédits - débits
        public decimal FluxNet { get; set; }

        /// Dernière date de mouvement constatée (période de référence)
        public DateTime? PeriodeReference { get; set; }
    }

    /// <summary>
    /// Répartition des actifs par devise.
    /// </summary>
    public class RepartitionDeviseDto
    {
        public string Devise { get; set; } = string.Empty;
        public int NombreComptes { get; set; }
        public decimal SoldeContreValeur { get; set; }
        public double Part { get; set; }
    }

    /// <summary>
    /// État des comptes : Créditeur / Soldé / Découvert.
    /// </summary>
    public class EtatCompteDto
    {
        public string Etat { get; set; } = string.Empty;
        public int Nombre { get; set; }
        public decimal SoldeContreValeur { get; set; }
    }

    /// <summary>
    /// Évolution mensuelle des flux (crédits / débits).
    /// </summary>
    public class EvolutionMouvementDto
    {
        public string Mois { get; set; } = string.Empty;
        public decimal Credits { get; set; }
        public decimal Debits { get; set; }
        public decimal FluxNet { get; set; }
    }

    /// <summary>
    /// Top des agences par encours et volume.
    /// </summary>
    public class TopAgenceDto
    {
        public string Agence { get; set; } = string.Empty;
        public int NombreComptes { get; set; }
        public decimal SoldeContreValeur { get; set; }
        public int NbMouvements { get; set; }
    }

    /// <summary>
    /// Derniers mouvements constatés (avec informations client).
    /// </summary>
    public class MouvementRecenteDto
    {
        public int Id { get; set; }
        public string NumCompte { get; set; } = string.Empty;
        public string Agence { get; set; } = string.Empty;
        public DateTime DateMvt { get; set; }
        public decimal Montant { get; set; }
        public string DevMVT { get; set; } = string.Empty;
        public string Sens { get; set; } = string.Empty;
        public decimal MontantTND { get; set; }
        public string? CodeClient { get; set; }
        public string? NomClient { get; set; }
    }

    /// <summary>
    /// Compte en situation de découvert.
    /// </summary>
    public class CompteDecouvertDto
    {
        public string NumCompte { get; set; } = string.Empty;
        public string Agence { get; set; } = string.Empty;
        public string Devise { get; set; } = string.Empty;
        public decimal Solde { get; set; }
        public decimal SoldeContreValeur { get; set; }
        public string? CodeClient { get; set; }
        public string? NomClient { get; set; }
    }
}
