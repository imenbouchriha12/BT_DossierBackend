using Backend.DTO.Historique;

namespace Backend.BL.IBL
{
    public interface IHistoriqueService
    {
        Task<PagedHistoriqueResult> SearchAsync(HistoriqueSearchDto search);
        Task<List<string>> GetTablesListAsync();
        Task<List<string>> GetActionsListAsync();
        Task EnregistrerAsync(string tableModifiee, int entiteId, string action, string? ancienneValeur, string? nouvelleValeur, int utilisateurId, string utilisateurNom);
    }
}
