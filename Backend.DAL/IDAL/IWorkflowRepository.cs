using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IWorkflowRepository
    {
        /// Retourne un workflow avec ses étapes, le document et le client lié
        Task<WorkflowValidation?> GetByIdAsync(int id);

        /// Retourne le workflow actif (EnCours) ou le dernier workflow d'un document
        Task<WorkflowValidation?> GetLatestByDocumentIdAsync(int documentId);

        /// Derniers workflows d'une liste de documents (un par document)
        Task<List<WorkflowValidation>> GetLatestByDocumentIdsAsync(IEnumerable<int> documentIds);

        /// Historique complet des workflows d'un document (du plus récent au plus ancien)
        Task<List<WorkflowValidation>> GetHistoryByDocumentIdAsync(int documentId);

        /// Workflows en attente pour les rôles donnés (inbox des validateurs)
        Task<(List<WorkflowValidation> Items, int Total)> GetEnAttenteByRolesAsync(List<string> roles, int page, int pageSize);

        /// Nombre de workflows en attente pour les rôles donnés
        Task<int> CountEnAttenteByRolesAsync(List<string> roles);

        /// Nombre de workflows en attente par rôle (tous confondus)
        Task<Dictionary<string, int>> CountEnAttenteParRoleAsync();

        Task AddAsync(WorkflowValidation workflow);

        Task SaveChangesAsync();
    }
}
