using Backend.BL.IBL;
using Backend.DTO.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{

    /// Contrôleur de gestion des rôles 
    /// 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// Liste tous les rôles
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _roleService.GetAllAsync();
            return Ok(roles);
        }

        /// Détail d'un rôle par id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var role = await _roleService.GetByIdAsync(id);
            if (role == null)
                return NotFound(new { message = "Role not found." });

            return Ok(role);
        }

        /// Crée un nouveau rôle
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
        {
            var role = await _roleService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
        }

        /// Met à jour le nom d'un rôle
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleDto dto)
        {
            var role = await _roleService.UpdateAsync(id, dto);
            return Ok(role);
        }

        /// Active un rôle
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            await _roleService.ActivateAsync(id);
            return Ok(new { message = "Role activated." });
        }

        /// Désactive un rôle
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            await _roleService.DeactivateAsync(id);
            return Ok(new { message = "Role deactivated." });
        }
    }
}
