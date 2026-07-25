namespace Backend.BL.IBL
{
    /// <summary>
    /// Interface représentant les méthodes que le serveur peut appeler sur les clients SignalR.
    /// Utilisée avec IHubContext&lt;NotificationHub, INotificationHubClient&gt; pour un typage fort.
    /// </summary>
    public interface INotificationHubClient
    {
        /// Notification générique reçue par le client Angular
        Task ReceiveNotification(object notification);

        /// Réponse à un ping de diagnostic
        Task Pong(DateTime timestamp);
    }
}
