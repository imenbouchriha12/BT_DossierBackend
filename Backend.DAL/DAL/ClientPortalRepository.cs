using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class ClientPortalRepository : IClientPortalRepository
    {
        private readonly AppDbContext _context;

        public ClientPortalRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ClientPortalToken?> GetActiveTokenByClientIdAsync(int idClient)
            => await _context.ClientPortalTokens
                .Include(t => t.Client)
                .Where(t => t.IdClient == idClient
                         && !t.EstRevoque
                         && t.DateExpiration > DateTime.UtcNow)
                .OrderByDescending(t => t.DateCreation)
                .FirstOrDefaultAsync();

        public async Task<ClientPortalToken?> GetByTokenValueAsync(string token)
            => await _context.ClientPortalTokens
                .Include(t => t.Client)
                .FirstOrDefaultAsync(t => t.Token == token);

        public async Task<List<ClientPortalToken>> GetByClientIdAsync(int idClient)
            => await _context.ClientPortalTokens
                .Where(t => t.IdClient == idClient)
                .OrderByDescending(t => t.DateCreation)
                .ToListAsync();

        public async Task AddAsync(ClientPortalToken token)
            => await _context.ClientPortalTokens.AddAsync(token);

        public void Update(ClientPortalToken token)
            => _context.ClientPortalTokens.Update(token);

        public async Task RevokeAllByClientIdAsync(int idClient)
        {
            var tokens = await _context.ClientPortalTokens
                .Where(t => t.IdClient == idClient && !t.EstRevoque)
                .ToListAsync();

            foreach (var t in tokens)
                t.EstRevoque = true;
        }

        public async Task<List<ClientPortalToken>> GetByAllClientsAsync()
            => await _context.ClientPortalTokens
                .OrderByDescending(t => t.DateCreation)
                .ToListAsync();

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}
