using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Événement de la timeline d'un client (fil d'activité).
    /// Consolide l'activité applicative : documents, statuts, comptes, rappels,
    /// notes, contacts et décisions de workflow.
    /// </summary>
    [Table("EvenementClient", Schema = "dbo")]
    public class EvenementClient
    {
        [Key]
        [Column("IdEvenement")]
        public int Id { get; set; }

        [Column("IdClient")]
        public int IdClient { get; set; }

        /// Catégorie d'événement : Client, Document, Statut, Compte, Rappel, Note, Contact, Workflow
        [Column("Type")]
        [MaxLength(30)]
        public string Type { get; set; } = string.Empty;

        /// Libellé affichable de l'événement
        [Column("Message")]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        /// Données contextuelles optionnelles (JSON, ex: ancien → nouveau statut)
        [Column("DonneesJson")]
        public string? DonneesJson { get; set; }

        /// Utilisateur à l'origine de l'événement (null si système / portail)
        [Column("UtilisateurId")]
        public int? UtilisateurId { get; set; }

        [Column("UtilisateurNom")]
        [MaxLength(200)]
        public string? UtilisateurNom { get; set; }

        [Column("DateEvenement")]
        public DateTime DateEvenement { get; set; } = DateTime.UtcNow;

        /// Identifiant de l'entité liée (document, workflow, rappel, note...)
        [Column("IdLien")]
        public int? IdLien { get; set; }

        /// Type de l'entité liée (ex: Document, WorkflowValidation, Reminder)
        [Column("TypeLien")]
        [MaxLength(30)]
        public string? TypeLien { get; set; }

        [ForeignKey("IdClient")]
        public Client Client { get; set; } = null!;

        [ForeignKey("UtilisateurId")]
        public User? Utilisateur { get; set; }
    }

    /// <summary>
    /// Constantes des types d'événements de la timeline client.
    /// </summary>
    public static class EvenementTypes
    {
        public const string Client = "Client";
        public const string Document = "Document";
        public const string Statut = "Statut";
        public const string Compte = "Compte";
        public const string Rappel = "Rappel";
        public const string Note = "Note";
        public const string Contact = "Contact";
        public const string Workflow = "Workflow";
    }
}
