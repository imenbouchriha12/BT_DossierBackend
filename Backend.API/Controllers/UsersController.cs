using Backend.BL.IBL;
using Backend.DTO.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.API.Controllers
{
    /// Contrôleur de gestion des utilisateurs 

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Superviseur")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public UsersController(IUserService userService, IEmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
        }

        /// Liste tous les utilisateurs
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        /// Détail d'un utilisateur par id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(user);
        }

        /// Liste les utilisateurs en attente d'activation
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var users = await _userService.GetPendingAsync();
            return Ok(users);
        }

        /// Approuve un utilisateur : active + assigne un rôle + envoie un email
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveUserDto dto)
        {
            await _userService.ApproveAsync(id, dto.RoleId);
            return Ok(new { message = "Utilisateur approuvé avec succès. Un email lui a été envoyé avec ses identifiants." });
        }

        /// Active un utilisateur
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            await _userService.ActivateAsync(id);
            return Ok(new { message = "User activated." });
        }

        /// Désactive un utilisateur
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            await _userService.DeactivateAsync(id);
            return Ok(new { message = "User deactivated." });
        }

        /// Assigne un rôle à un utilisateur
        [HttpPost("{userId}/roles/{roleId}")]
        public async Task<IActionResult> AssignRole(int userId, int roleId)
        {
            await _userService.AssignRoleAsync(userId, roleId);
            return Ok(new { message = "Role assigned." });
        }

        /// Retire un rôle d'un utilisateur
        [HttpDelete("{userId}/roles/{roleId}")]
        public async Task<IActionResult> RemoveRole(int userId, int roleId)
        {
            await _userService.RemoveRoleAsync(userId, roleId);
            return Ok(new { message = "Role removed." });
        }

        /// Met à jour les informations d'un utilisateur
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _userService.UpdateAsync(id, dto);
            return Ok(user);
        }

        /// Supprime un utilisateur
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteAsync(id);
            return Ok(new { message = "User deleted." });
        }

        /// Envoie un email à un utilisateur
        [HttpPost("{id}/send-email")]
        public async Task<IActionResult> SendEmail(int id, [FromBody] SendEmailDto dto)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            await _emailService.SendDirectEmailAsync(user.Email, user.Username, dto.Subject, dto.Body);
            return Ok(new { message = "Email envoyé avec succès." });
        }
    }
}
