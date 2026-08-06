using Backend.DAL.Entities;
using Backend.DTO.Client;
using Backend.DTO.Workflow;

namespace Backend.BL.IBL
{
    public interface IWorkflowService
    {
        /// Crée automatiquement un workflow de validation pour un document nouvellement soumis
        Task<WorkflowValidationDto> CreateForDocumentAsync(DocumentClient document, int? createdByUserId, string? createdByName);

        Task<WorkflowValidationDto?> GetByIdAsync(int id);

        /// Workflow le plus récent d'un document
        Task<WorkflowValidationDto?> GetByDocumentIdAsync(int documentId);

        /// Derniers workflows d'une liste de documents (un par document)
        Task<List<WorkflowValidationDto>> GetByDocumentIdsAsync(IEnumerable<int> documentIds);

        /// Historique complet des validations d'un document
        Task<List<WorkflowValidationDto>> GetHistoryByDocumentIdAsync(int documentId);

        /// Inbox : workflows en attente pour l'utilisateur connecté (selon ses rôles)
        Task<PagedResult<WorkflowValidationDto>> GetEnAttenteAsync(int page, int pageSize);

        Task<int> GetEnAttenteCountAsync();

        Task<WorkflowStatsDto> GetStatsAsync();

        /// Valide l'étape courante du workflow (le rôle de l'utilisateur doit correspondre)
        Task<WorkflowValidationDto> ValiderAsync(int workflowId, string? commentaire);

        /// Rejette le workflow (motif obligatoire)
        Task<WorkflowValidationDto> RejeterAsync(int workflowId, string commentaire);

        /// Relance un workflow rejeté (nouvelle instance de workflow, historique conservé)
        Task<WorkflowValidationDto> RelancerAsync(int workflowId, string? commentaire);
    }
}
