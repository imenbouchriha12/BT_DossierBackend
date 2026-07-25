using System.ComponentModel.DataAnnotations;

namespace Backend.DTO.Auth
{
    public class ApproveUserDto
    {
        [Required]
        public int RoleId { get; set; }
    }
}
