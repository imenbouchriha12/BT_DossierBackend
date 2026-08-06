using System.Collections.Concurrent;

namespace Backend.BL.BL
{
    /// Registre en mémoire des utilisateurs connectés à la messagerie (SignalR).
    /// La clé est l'identifiant utilisateur, la valeur le nombre de connexions simultanées.
    public static class PresenceUtilisateurs
    {
        private static readonly ConcurrentDictionary<int, int> _connexions = new();

        public static void Connecter(int userId)
        {
            _connexions.AddOrUpdate(userId, 1, (_, count) => count + 1);
        }

        public static void Deconnecter(int userId)
        {
            if (_connexions.TryGetValue(userId, out var count))
            {
                if (count <= 1)
                    _connexions.TryRemove(userId, out _);
                else
                    _connexions[userId] = count - 1;
            }
        }

        public static bool EstEnLigne(int userId) => _connexions.ContainsKey(userId);

        public static IReadOnlyCollection<int> UtilisateursEnLigne() => _connexions.Keys.ToList();
    }
}
