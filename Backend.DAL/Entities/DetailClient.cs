using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    [Table("DetailClients", Schema = "dbo")]
    public class DetailClient
    {
        [Key]
        [Column("IdDetail")]
        public int Id { get; set; }

        [Column("IdClient")]
        public int IdClient { get; set; }

        [Column("Profession")]
        public string Profession { get; set; } = string.Empty;

        [Column("Employeur")]
        public string Employeur { get; set; } = string.Empty;

        [Column("SecteurActivite")]
        public string SecteurActivite { get; set; } = string.Empty;

        [Column("RevenuMensuel")]
        public decimal RevenuMensuel { get; set; }

        [Column("SituationFamiliale")]
        public string SituationFamiliale { get; set; } = string.Empty;

        [Column("TypeResidence")]
        public string TypeResidence { get; set; } = string.Empty;

        [ForeignKey("IdClient")]
        public Client Client { get; set; } = null!;
    }
}
