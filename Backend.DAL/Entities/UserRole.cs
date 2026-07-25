using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// Association many-to-many entre User et Role
    [Table("AppReg_UserRoles", Schema = "dbo")]
    public class UserRole
    {
        /// Clé primaire composite
        [Column("UserRoleId")]
        public int UserRoleId { get; set; }

        /// FK vers l'utilisateur
        [Column("UserId")]
        public int UserId { get; set; }

        /// Navigation : utilisateur associé
        public User User { get; set; } = null!;

        /// FK vers le rôle
        [Column("RoleId")]
        public int RoleId { get; set; }

        /// Navigation : rôle associé
        public Role Role { get; set; } = null!;

        /// Date d'assignation du rôle
        [Column("AssignedAt")]
        public DateTime? AssignedAt { get; set; }
    }
}
