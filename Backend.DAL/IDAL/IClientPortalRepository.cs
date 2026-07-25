using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IClientPortalRepository
    {
        /// Retourne le token actif (non révoqué, non expiré) pour un client
        Task<ClientPortalToken?> GetActiveTokenByClientIdAsync(int idClient);

        /// Trouve un token par sa valeur (pour validation d'accès portail)
        Task<ClientPortalToken?> GetByTokenValueAsync(string token);

        /// Tous les tokens d'un client (historique)
        Task<List<ClientPortalToken>> GetByClientIdAsync(int idClient);

        Task AddAsync(ClientPortalToken token);
        void Update(ClientPortalToken token);

        /// Révoque tous les tokens actifs d'un client (avant d'en générer un nouveau)
        Task RevokeAllByClientIdAsync(int idClient);

        /// Retourne le dernier token (actif ou non) pour tous les clients — utilisé pour le dashboard
        Task<List<ClientPortalToken>> GetByAllClientsAsync();

        Task SaveChangesAsync();
    }
}
