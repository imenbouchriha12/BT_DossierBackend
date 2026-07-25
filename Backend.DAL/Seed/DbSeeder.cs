using Backend.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.Seed
{
    /// <summary>Seedeur pour créer le premier administrateur au démarrage.</summary>
    public static class DbSeeder
    {
        /// <summary>Crée un compte admin par défaut si aucun admin n'existe.</summary>
        public static async Task SeedAdminAsync(AppDbContext context, string adminPassword = "Admin@123")
        {
            var adminExists = await context.Users
                .Include(u => u.UserRoles)
                .AnyAsync(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin"));

            if (adminExists) return;

            var admin = new User
            {
                Matricule = "ADMIN001",
                Username = "Administrateur",
                Email = "admin@backend.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            // Assigne le rôle Admin
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            context.UserRoles.Add(new UserRole
            {
                UserId = admin.Id,
                RoleId = adminRole.Id,
                AssignedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }
    }
}
