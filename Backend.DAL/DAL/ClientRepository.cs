using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections;

namespace Backend.DAL.DAL
{
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;

        public ClientRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Client?> GetByIdAsync(int id)
        {
            return await _context.Clients.FindAsync(id);
        }

        public async Task<Client?> GetByIdWithDetailAsync(int id)
        {
            return await _context.Clients
                .Include(c => c.Detail)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Client>> GetAllAsync()
        {
            return await _context.Clients
                .Include(c => c.Detail)
                .Where(c => !c.EstArchive)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<(List<Client> Items, int Total)> SearchAsync(
            string? codeClient, string? nom, string? prenom,
            string? ville, StatutClient? statut, DateTime? dateDebut, DateTime? dateFin,
            int page, int pageSize)
        {
            var query = _context.Clients
                .Include(c => c.Detail)
                .Where(c => !c.EstArchive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(codeClient))
                query = query.Where(c => c.CodeClient.Contains(codeClient));
            if (!string.IsNullOrWhiteSpace(nom))
                query = query.Where(c => c.Nom.Contains(nom));
            if (!string.IsNullOrWhiteSpace(prenom))
                query = query.Where(c => c.Prenom.Contains(prenom));
            if (!string.IsNullOrWhiteSpace(ville))
                query = query.Where(c => c.Ville == ville);
            if (statut.HasValue)
                query = query.Where(c => c.Statut == statut.Value);
            if (dateDebut.HasValue)
                query = query.Where(c => c.DateCreation >= dateDebut.Value);
            if (dateFin.HasValue)
                query = query.Where(c => c.DateCreation <= dateFin.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.DateCreation)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<Client> Items, int Total)> GetArchivedAsync(int page, int pageSize)
        {
            var query = _context.Clients
                .Include(c => c.Detail)
                .Where(c => c.EstArchive)
                .OrderByDescending(c => c.DateCreation);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<bool> CodeClientExistsAsync(string codeClient)
        {
            return await _context.Clients.AnyAsync(c => c.CodeClient == codeClient);
        }

        public async Task AddAsync(Client client)
        {
            await _context.Clients.AddAsync(client);
        }

        public void Delete(Client client)
        {
            _context.Clients.Remove(client);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _context.Clients.CountAsync(c => !c.EstArchive);
        }

        public async Task<int> CountArchivedAsync()
        {
            return await _context.Clients.CountAsync(c => c.EstArchive);
        }

        public async Task<int> CountByStatutAsync(StatutClient statut)
        {
            return await _context.Clients.CountAsync(c => c.Statut == statut && !c.EstArchive);
        }

        public async Task<int> CountByDateRangeAsync(DateTime debut, DateTime fin)
        {
            return await _context.Clients.CountAsync(c => c.DateCreation >= debut && c.DateCreation <= fin && !c.EstArchive);
        }

        public async Task<List<string>> GetDistinctVillesAsync()
        {
            return await _context.Clients
                .Where(c => !c.EstArchive && c.Ville != null && c.Ville != "")
                .Select(c => c.Ville)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();
        }

        public async Task<List<IGrouping<string, Client>>> GroupByVilleAsync()
        {
            return await _context.Clients
                .Where(c => !c.EstArchive)
                .GroupBy(c => c.Ville)
                .ToListAsync();
        }

        public async Task<List<IGrouping<StatutClient, Client>>> GroupByStatutAsync()
        {
            return await _context.Clients
                .Where(c => !c.EstArchive)
                .GroupBy(c => c.Statut)
                .ToListAsync();
        }

        public async Task<List<IGrouping<string, Client>>> GroupByMonthAsync()
        {
            var groupes = await _context.Clients
                .Where(c => !c.EstArchive)
                .GroupBy(c => new { c.DateCreation.Year, c.DateCreation.Month })
                .ToListAsync();

            var result = new List<IGrouping<string, Client>>();
            foreach (var g in groupes)
            {
                var key = $"{g.Key.Year:D4}-{g.Key.Month:D2}";
                result.Add(new ClientGrouping(key, g.ToList()));
            }
            return result;
        }

        public async Task<int> CountBySexeAsync(string sexe)
        {
            return await _context.Clients.CountAsync(c => c.Sexe == sexe && !c.EstArchive);
        }

        public async Task<List<Client>> GetRecentAsync(int count)
        {
            return await _context.Clients
                .Where(c => !c.EstArchive)
                .OrderByDescending(c => c.DateCreation)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }

    internal class ClientGrouping : IGrouping<string, Client>
    {
        public string Key { get; }
        private readonly List<Client> _clients;

        public ClientGrouping(string key, List<Client> clients)
        {
            Key = key;
            _clients = clients;
        }

        public IEnumerator<Client> GetEnumerator() => _clients.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _clients.GetEnumerator();
    }
}
