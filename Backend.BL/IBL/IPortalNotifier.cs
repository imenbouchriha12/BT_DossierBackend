namespace Backend.BL.IBL
{
    /// <summary>
    /// Abstraction pour envoyer des notifications temps réel via SignalR.
    /// Implémentée dans Backend.API pour éviter la dépendance circulaire BL → API.
    /// </summary>
    public interface IPortalNotifier
    {
        /// Envoie une notification push à un utilisateur interne spécifique
        Task NotifyUserAsync(int userId, string titre, string message, string type, string lien);

        /// Envoie une notification push à tous les utilisateurs d'un groupe (ex: "Admins")
        Task NotifyGroupAsync(string groupName, string titre, string message, string type, string lien);
    }
}
