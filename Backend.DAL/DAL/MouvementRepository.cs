using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class MouvementRepository : IMouvementRepository
    {
        private readonly AppDbContext _context;

        public MouvementRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Mouvement> Items, int Total)> GetByCompteAsync(string numCompte, string agence, DateTime? dateDebut, DateTime? dateFin, string? sens, decimal? montantMin, decimal? montantMax, int page, int pageSize)
        {
            var query = _context.Mouvements.Where(m => m.NumCompte == numCompte && m.Agence == agence).AsQueryable();
            query = ApplyFiltres(query, dateDebut, dateFin, sens, montantMin, montantMax);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.DateMvt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<Mouvement> Items, int Total)> GetByClientAsync(int idClient, DateTime? dateDebut, DateTime? dateFin, string? sens, decimal? montantMin, decimal? montantMax, int page, int pageSize)
        {
            var query =
                from m in _context.Mouvements
                join c in _context.Comptes on new { m.NumCompte, m.Agence } equals new { c.NumCompte, c.Agence }
                where c.IdClient == idClient
                select m;

            query = ApplyFiltres(query, dateDebut, dateFin, sens, montantMin, montantMax);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.DateMvt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<decimal> GetTotalDebitAsync(int idClient)
        {
            return await (
                from m in _context.Mouvements
                join c in _context.Comptes on new { m.NumCompte, m.Agence } equals new { c.NumCompte, c.Agence }
                where c.IdClient == idClient && m.Sens == "D"
                select (decimal?)m.Montant).SumAsync() ?? 0;
        }

        public async Task<decimal> GetTotalCreditAsync(int idClient)
        {
            return await (
                from m in _context.Mouvements
                join c in _context.Comptes on new { m.NumCompte, m.Agence } equals new { c.NumCompte, c.Agence }
                where c.IdClient == idClient && m.Sens == "C"
                select (decimal?)m.Montant).SumAsync() ?? 0;
        }

        public async Task<int> CountByClientAsync(int idClient)
        {
            return await (
                from m in _context.Mouvements
                join c in _context.Comptes on new { m.NumCompte, m.Agence } equals new { c.NumCompte, c.Agence }
                where c.IdClient == idClient
                select m).CountAsync();
        }

        public async Task<List<Mouvement>> GetByClientAllAsync(int idClient, DateTime? dateDebut)
        {
            var query =
                from m in _context.Mouvements
                join c in _context.Comptes on new { m.NumCompte, m.Agence } equals new { c.NumCompte, c.Agence }
                where c.IdClient == idClient
                select m;

            if (dateDebut.HasValue)
                query = query.Where(m => m.DateMvt >= dateDebut.Value);

            return await query
                .OrderByDescending(m => m.DateMvt)
                .ToListAsync();
        }

        public async Task<List<(DateTime Jour, decimal TotalDebit)>> GetDebitQuotidiensAsync(int idClient, DateTime? dateDebut)
        {
            var query =
                from m in _context.Mouvements
                join c in _context.Comptes on new { m.NumCompte, m.Agence } equals new { c.NumCompte, c.Agence }
                where c.IdClient == idClient && m.Sens == "D"
                select m;

            if (dateDebut.HasValue)
                query = query.Where(m => m.DateMvt >= dateDebut.Value);

            var resultats = await query
                .GroupBy(m => m.DateMvt.Date)
                .Select(g => new { Jour = g.Key, Total = g.Sum(m => m.Montant) })
                .ToListAsync();

            return resultats.Select(r => (r.Jour, r.Total)).ToList();
        }

        private static IQueryable<Mouvement> ApplyFiltres(IQueryable<Mouvement> query, DateTime? dateDebut, DateTime? dateFin, string? sens, decimal? montantMin, decimal? montantMax)
        {
            if (dateDebut.HasValue)
                query = query.Where(m => m.DateMvt >= dateDebut.Value);
            if (dateFin.HasValue)
                query = query.Where(m => m.DateMvt <= dateFin.Value);
            if (!string.IsNullOrWhiteSpace(sens))
                query = query.Where(m => m.Sens == sens);
            if (montantMin.HasValue)
                query = query.Where(m => m.Montant >= montantMin.Value);
            if (montantMax.HasValue)
                query = query.Where(m => m.Montant <= montantMax.Value);
            return query;
        }
    }
}
