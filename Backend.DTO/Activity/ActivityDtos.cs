namespace Backend.DTO.Activity
{
    /// Événement consolidé de la timeline client
    public class EvenementClientDto
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? DonneesJson { get; set; }
        public int? UtilisateurId { get; set; }
        public string? UtilisateurNom { get; set; }
        public DateTime DateEvenement { get; set; }
        public int? IdLien { get; set; }
        public string? TypeLien { get; set; }
    }

    public class NoteClientDto
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public string? Titre { get; set; }
        public string Contenu { get; set; } = string.Empty;
        public string Categorie { get; set; } = "Général";
        public int CreeParUserId { get; set; }
        public string? CreeParNom { get; set; }
        public DateTime DateCreation { get; set; }
        public int? ModifieParUserId { get; set; }
        public string? ModifieParNom { get; set; }
        public DateTime? DateMaj { get; set; }
        public bool EstEpinglee { get; set; }
    }

    public class CreateNoteClientDto
    {
        public int IdClient { get; set; }
        public string? Titre { get; set; }
        public string Contenu { get; set; } = string.Empty;
        public string Categorie { get; set; } = "Général";
        public bool EstEpinglee { get; set; }
    }

    public class UpdateNoteClientDto
    {
        public string? Titre { get; set; }
        public string? Contenu { get; set; }
        public string? Categorie { get; set; }
        public bool? EstEpinglee { get; set; }
    }

    public class ContactClientDto
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public string TypeContact { get; set; } = "Autre";
        public string Nom { get; set; } = string.Empty;
        public string? Prenom { get; set; }
        public string? Telephone { get; set; }
        public string? Email { get; set; }
        public string? Adresse { get; set; }
        public string? Relation { get; set; }
        public DateTime DateCreation { get; set; }
    }

    public class CreateContactClientDto
    {
        public int IdClient { get; set; }
        public string TypeContact { get; set; } = "Autre";
        public string Nom { get; set; } = string.Empty;
        public string? Prenom { get; set; }
        public string? Telephone { get; set; }
        public string? Email { get; set; }
        public string? Adresse { get; set; }
        public string? Relation { get; set; }
    }

    public class UpdateContactClientDto
    {
        public string? TypeContact { get; set; }
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public string? Telephone { get; set; }
        public string? Email { get; set; }
        public string? Adresse { get; set; }
        public string? Relation { get; set; }
    }
}
