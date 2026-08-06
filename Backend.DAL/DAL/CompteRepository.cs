using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class CompteRepository : ICompteRepository
    {
        private readonly AppDbContext _context;

        public CompteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Compte?> GetByIdAsync(int id)
        {
            return await _context.Comptes.FindAsync(id);
        }

        public async Task<List<Compte>> GetByClientIdAsync(int idClient)
        {
            return await _context.Comptes
                .Where(c => c.IdClient == idClient)
                .OrderBy(c => c.Devise)
                .ToListAsync();
        }

        public async Task<Compte?> GetByNumeroAgenceAsync(string numCompte, string agence)
        {
            return await _context.Comptes
                .FirstOrDefaultAsync(c => c.NumCompte == numCompte && c.Agence == agence);
        }

        public async Task<List<Compte>> GetAllAsync()
        {
            return await _context.Comptes.OrderBy(c => c.NumCompte).ToListAsync();
        }

        public async Task<List<Compte>> GetByIdsAsync(IEnumerable<int> idClients)
        {
            var ids = idClients.ToList();
            return await _context.Comptes
                .Where(c => ids.Contains(c.IdClient))
                .OrderBy(c => c.IdClient)
                .ThenBy(c => c.Devise)
                .ToListAsync();
        }

        public async Task<(List<CompteSearchItem> Items, int Total)> SearchAsync(
            string? numCompte, string? agence, string? devise, string? codeClient, int page, int pageSize)
        {
            var query =
                from c in _context.Comptes
                join cl in _context.Clients on c.IdClient equals cl.Id into cj
                from cl in cj.DefaultIfEmpty()
                select new CompteSearchItem
                {
                    Id = c.Id,
                    IdClient = c.IdClient,
                    NumCompte = c.NumCompte,
                    Agence = c.Agence,
                    Devise = c.Devise,
                    Solde = c.Solde,
                    SoldeContreValeur = c.SoldeContreValeur,
                    DateOuverture = c.DateOuverture,
                    CodeClient = cl == null ? null : cl.CodeClient,
                    NomClient = cl == null ? null : (cl.Nom + " " + cl.Prenom).Trim()
                };

            if (!string.IsNullOrWhiteSpace(numCompte))
                query = query.Where(x => x.NumCompte.Contains(numCompte));
            if (!string.IsNullOrWhiteSpace(agence))
                query = query.Where(x => x.Agence == agence);
            if (!string.IsNullOrWhiteSpace(devise))
                query = query.Where(x => x.Devise == devise);
            if (!string.IsNullOrWhiteSpace(codeClient))
                query = query.Where(x => x.CodeClient == codeClient);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.NumCompte)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}
