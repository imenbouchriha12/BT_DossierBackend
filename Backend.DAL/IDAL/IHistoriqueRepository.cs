using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IHistoriqueRepository
    {
        Task AddAsync(HistoriqueModification historique);
        Task<HistoriqueModification?> GetByIdAsync(int id);
        Task<(List<HistoriqueModification> Items, int Total)> SearchAsync(
            string? tableModifiee, string? utilisateurNom, string? action,
            DateTime? dateDebut, DateTime? dateFin, int page, int pageSize);
        Task SaveChangesAsync();
    }
}
