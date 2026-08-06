using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Historique;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    public class HistoriqueService : IHistoriqueService
    {
        private readonly IHistoriqueRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<HistoriqueService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public HistoriqueService(IHistoriqueRepository repository, IMapper mapper, ILogger<HistoriqueService> logger, ICurrentUserService currentUserService)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<PagedHistoriqueResult> SearchAsync(HistoriqueSearchDto search)
        {
            _logger.LogInformation("Recherche d'historique avec les critères fournis");

            // Admin voit tout ; les autres (Superviseur, Agent) ne voient que leur propre activité.
            int? userId = _currentUserService.IsAdmin ? null : _currentUserService.UserId;

            var (items, total) = await _repository.SearchAsync(
                search.TableModifiee, search.UtilisateurNom, search.Action,
                search.DateDebut, search.DateFin, search.Page, search.PageSize, userId);

            return new PagedHistoriqueResult
            {
                Items = _mapper.Map<List<HistoriqueDto>>(items),
                Total = total,
                Page = search.Page,
                PageSize = search.PageSize
            };
        }

        public async Task<List<string>> GetTablesListAsync()
        {
            return await Task.FromResult(new List<string> { "Clients", "Documents", "Utilisateurs" });
        }

        public async Task<List<string>> GetActionsListAsync()
        {
            return await Task.FromResult(new List<string> { "Création", "Modification", "Suppression", "Archivage", "Restauration", "Activation", "Désactivation" });
        }

        public async Task EnregistrerAsync(string tableModifiee, int entiteId, string action, string? ancienneValeur, string? nouvelleValeur, int utilisateurId, string utilisateurNom)
        {
            var historique = new HistoriqueModification
            {
                TableModifiee = tableModifiee,
                EntiteId = entiteId,
                Action = action,
                AncienneValeur = ancienneValeur,
                NouvelleValeur = nouvelleValeur,
                UtilisateurId = utilisateurId,
                UtilisateurNom = utilisateurNom,
                DateHeure = DateTime.UtcNow
            };

            await _repository.AddAsync(historique);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Historique enregistré : {Action} sur {Table} #{EntiteId} par {User}", action, tableModifiee, entiteId, utilisateurNom);
        }
    }
}
