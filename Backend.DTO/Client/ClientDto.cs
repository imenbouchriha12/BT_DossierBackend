using System.ComponentModel.DataAnnotations;

namespace Backend.DTO.Client
{
    public class ClientDto
    {
        public int Id { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public DateTime DateNaissance { get; set; }
        public string Sexe { get; set; } = string.Empty;
        public string Nationalite { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public string Ville { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public string Statut { get; set; } = string.Empty;
        public bool EstArchive { get; set; }
        public string? MotifBlocage { get; set; }
        public string TypeClient { get; set; } = "Particulier";
        public string? MatriculeFiscal { get; set; }
        public string? RegistreCommerce { get; set; }
        public DetailClientDto? Detail { get; set; }
    }

    public class DetailClientDto
    {
        public int Id { get; set; }
        public string Profession { get; set; } = string.Empty;
        public string Employeur { get; set; } = string.Empty;
        public string SecteurActivite { get; set; } = string.Empty;
        public decimal RevenuMensuel { get; set; }
        public string SituationFamiliale { get; set; } = string.Empty;
        public string TypeResidence { get; set; } = string.Empty;
    }

    public class CreateClientDto
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100, MinimumLength = 2)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est requis")]
        [StringLength(100, MinimumLength = 2)]
        public string Prenom { get; set; } = string.Empty;

        [Required]
        public DateTime DateNaissance { get; set; }

        [Required(ErrorMessage = "Le sexe est requis")]
        public string Sexe { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nationalité est requise")]
        public string Nationalite { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone est requis")]
        [Phone(ErrorMessage = "Numéro de téléphone invalide")]
        public string Telephone { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse est requise")]
        public string Adresse { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ville est requise")]
        public string Ville { get; set; } = string.Empty;

        public string Statut { get; set; } = "Actif";
        public string TypeClient { get; set; } = "Particulier";
        public string? MatriculeFiscal { get; set; }
        public string? RegistreCommerce { get; set; }
        public CreateDetailClientDto? Detail { get; set; }
    }

    public class CreateDetailClientDto
    {
        public string Profession { get; set; } = string.Empty;
        public string Employeur { get; set; } = string.Empty;
        public string SecteurActivite { get; set; } = string.Empty;
        public decimal RevenuMensuel { get; set; }
        public string SituationFamiliale { get; set; } = string.Empty;
        public string TypeResidence { get; set; } = string.Empty;
    }

    public class UpdateClientDto
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100, MinimumLength = 2)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est requis")]
        [StringLength(100, MinimumLength = 2)]
        public string Prenom { get; set; } = string.Empty;

        [Required]
        public DateTime DateNaissance { get; set; }

        [Required]
        public string Sexe { get; set; } = string.Empty;

        [Required]
        public string Nationalite { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Telephone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Adresse { get; set; } = string.Empty;

        [Required]
        public string Ville { get; set; } = string.Empty;

        public string Statut { get; set; } = string.Empty;
        public string TypeClient { get; set; } = "Particulier";
        public string? MatriculeFiscal { get; set; }
        public string? RegistreCommerce { get; set; }
        public UpdateDetailClientDto? Detail { get; set; }
    }

    public class UpdateDetailClientDto
    {
        public string Profession { get; set; } = string.Empty;
        public string Employeur { get; set; } = string.Empty;
        public string SecteurActivite { get; set; } = string.Empty;
        public decimal RevenuMensuel { get; set; }
        public string SituationFamiliale { get; set; } = string.Empty;
        public string TypeResidence { get; set; } = string.Empty;
    }

    public class ClientSearchDto
    {
        public string? CodeClient { get; set; }
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public string? Ville { get; set; }
        public string? Statut { get; set; }
        public string? TypeClient { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// Types de clients gérés par l'application (détermine aussi les documents requis).
    /// </summary>
    public static class ClientTypes
    {
        public const string Particulier = "Particulier";
        public const string Entreprise = "Entreprise";
        public const string VIP = "VIP";
        public const string Professionnel = "Professionnel";
        public const string Association = "Association";

        public static readonly string[] All =
        {
            Particulier, Entreprise, VIP, Professionnel, Association
        };

        public static bool IsValid(string? type)
            => !string.IsNullOrWhiteSpace(type) && All.Contains(type.Trim());
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    }

    public class ClientStatsDto
    {
        public int TotalClients { get; set; }
        public int ClientsActifs { get; set; }
        public int ClientsEnAttente { get; set; }
        public int ClientsBloques { get; set; }
        public int NouveauxClientsMois { get; set; }
        public int Hommes { get; set; }
        public int Femmes { get; set; }
        public int TotalDocuments { get; set; }
        public int DocumentsValides { get; set; }
        public int DocumentsEnAttente { get; set; }
        public int DocumentsRejetes { get; set; }
        public List<VilleStat> RepartitionVille { get; set; } = new();
        public List<StatutStat> RepartitionStatut { get; set; } = new();
        public List<TypeStat> RepartitionType { get; set; } = new();
        public List<CreationMois> CreationsParMois { get; set; } = new();
        public List<ClientRecentsItem> ClientsRecents { get; set; } = new();
        public List<DocumentRecentsItem> DocumentsRecents { get; set; } = new();
    }

    public class DocumentRecentsItem
    {
        public int Id { get; set; }
        public string TypeDocument { get; set; } = string.Empty;
        public string NomFichier { get; set; } = string.Empty;
        public string StatutValidation { get; set; } = string.Empty;
        public string CodeClient { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public DateTime DateAjout { get; set; }
    }

    public class VilleStat
    {
        public string Ville { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class StatutStat
    {
        public string Statut { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TypeStat
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class CreationMois
    {
        public string Mois { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ClientRecentsItem
    {
        public int Id { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Ville { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
    }
}
