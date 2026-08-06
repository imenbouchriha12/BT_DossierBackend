using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface ISynchronisationRepository
    {
        Task<SynchronisationLog?> GetLastAsync();
        Task<SynchronisationLog?> GetEnCoursAsync();
        Task<List<SynchronisationLog>> GetHistoryAsync(int page, int pageSize);
        Task<List<SynchronisationRejet>> GetRejetsAsync(int idSynchronisation);
        Task AddLogAsync(SynchronisationLog log);
        Task AddRejetsAsync(IEnumerable<SynchronisationRejet> rejets);
        Task<int> CountRejetsNonResolusAsync();
        Task SaveChangesAsync();
    }
}
