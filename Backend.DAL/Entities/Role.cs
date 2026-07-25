using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// Entité représentant un rôle
    [Table("AppReg_Roles", Schema = "dbo")]
    public class Role
    {
        /// Identifiant unique
        [Column("RoleId")]
        public int Id { get; set; }

        /// Nom du rôle
        [Column("RoleName")]
        public string Name { get; set; } = string.Empty;

        /// Indique si le rôle est actif
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        /// Navigation : utilisateurs possédant ce rôle
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
