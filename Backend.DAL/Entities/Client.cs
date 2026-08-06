using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    [Table("Client", Schema = "dbo")]
    public class Client
    {
        [Key]
        [Column("IdClient")]
        public int Id { get; set; }

        [Column("CodeClient")]
        public string CodeClient { get; set; } = string.Empty;

        [Column("Nom")]
        public string Nom { get; set; } = string.Empty;

        [Column("Prenom")]
        public string Prenom { get; set; } = string.Empty;

        [Column("DateNaissance")]
        public DateTime DateNaissance { get; set; }

        [Column("Sexe")]
        public string Sexe { get; set; } = string.Empty;

        [Column("Nationalite")]
        public string Nationalite { get; set; } = string.Empty;

        [Column("Telephone")]
        public string Telephone { get; set; } = string.Empty;

        [Column("Email")]
        public string Email { get; set; } = string.Empty;

        [Column("Adresse")]
        public string Adresse { get; set; } = string.Empty;

        [Column("Ville")]
        public string Ville { get; set; } = string.Empty;

        [Column("DateCreation")]
        public DateTime DateCreation { get; set; }

        [Column("Statut")]
        public StatutClient Statut { get; set; } = StatutClient.Actif;

        [Column("EstArchive")]
        public bool EstArchive { get; set; } = false;

        [Column("MotifBlocage")]
        [MaxLength(500)]
        public string? MotifBlocage { get; set; }

        /// Type de client : détermine les documents requis (ex: Particulier, Entreprise, VIP, Professionnel, Association)
        [Column("TypeClient")]
        [MaxLength(50)]
        public string TypeClient { get; set; } = "Particulier";

        /// Matricule fiscal (Entreprise / Association / Professionnel)
        [Column("MatriculeFiscal")]
        [MaxLength(50)]
        public string? MatriculeFiscal { get; set; }

        /// Registre de commerce (Entreprise / Association)
        [Column("RegistreCommerce")]
        [MaxLength(50)]
        public string? RegistreCommerce { get; set; }

        /// Client issu du référentiel SISS (importé par la synchronisation)
        [Column("EstImporteSISS")]
        public bool EstImporteSISS { get; set; } = false;

        /// Dernière synchronisation réussie pour ce client
        [Column("DateDerniereSynchroSISS")]
        public DateTime? DateDerniereSynchroSISS { get; set; }

        /// Empreinte SHA-256 des champs gérés par le SISS (détection de changement)
        [Column("HashReferentiel")]
        [MaxLength(64)]
        public string? HashReferentiel { get; set; }

        public DetailClient? Detail { get; set; }
    }
}
