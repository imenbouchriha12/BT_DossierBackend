using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IMouvementRepository
    {
        Task<(List<Mouvement> Items, int Total)> GetByCompteAsync(string numCompte, string agence, DateTime? dateDebut, DateTime? dateFin, string? sens, decimal? montantMin, decimal? montantMax, int page, int pageSize);
        Task<(List<Mouvement> Items, int Total)> GetByClientAsync(int idClient, DateTime? dateDebut, DateTime? dateFin, string? sens, decimal? montantMin, decimal? montantMax, int page, int pageSize);
        Task<decimal> GetTotalDebitAsync(int idClient);
        Task<decimal> GetTotalCreditAsync(int idClient);
        Task<int> CountByClientAsync(int idClient);
        Task<List<Mouvement>> GetByClientAllAsync(int idClient, DateTime? dateDebut);
        Task<List<(DateTime Jour, decimal TotalDebit)>> GetDebitQuotidiensAsync(int idClient, DateTime? dateDebut);
    }
}
