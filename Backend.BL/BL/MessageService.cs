using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Message;

namespace Backend.BL.BL
{
    /// Messagerie privée en temps réel entre les utilisateurs (Admin, Agent, Superviseur).
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;

        public MessageService(IMessageRepository messageRepository, IUserRepository userRepository)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        public async Task<List<ConversationDto>> GetConversationsAsync(int userId)
        {
            var messages = await _messageRepository.GetAllForUserAsync(userId);

            var idPartenaires = messages
                .Select(m => m.ExpediteurUserId == userId ? m.DestinataireUserId : m.ExpediteurUserId)
                .Distinct()
                .ToList();

            var utilisateurs = await _userRepository.GetAllWithRolesAsync();
            var mapUtilisateurs = utilisateurs.ToDictionary(u => u.Id, u => u);

            var conversations = new List<ConversationDto>();

            foreach (var idPartenaire in idPartenaires)
            {
                if (!mapUtilisateurs.TryGetValue(idPartenaire, out var partenaire))
                    continue;

                var echanges = messages
                    .Where(m => (m.ExpediteurUserId == userId && m.DestinataireUserId == idPartenaire) ||
                                (m.ExpediteurUserId == idPartenaire && m.DestinataireUserId == userId))
                    .ToList();

                var dernier = echanges.OrderByDescending(m => m.DateEnvoi).FirstOrDefault();
                var nonLu = echanges.Count(m => m.DestinataireUserId == userId && !m.Lu);

                conversations.Add(new ConversationDto
                {
                    AutreUserId = idPartenaire,
                    AutreNom = partenaire.Username,
                    AutreMatricule = partenaire.Matricule,
                    AutreRole = partenaire.UserRoles.FirstOrDefault()?.Role?.Name ?? string.Empty,
                    EnLigne = PresenceUtilisateurs.EstEnLigne(idPartenaire),
                    DernierMessage = ApercuDernierMessage(dernier),
                    DerniereDate = dernier?.DateEnvoi,
                    NonLu = nonLu
                });
            }

            return conversations.OrderByDescending(c => c.DerniereDate).ToList();
        }

        public async Task<ConversationPagedDto> GetMessagesAsync(int userId, int otherUserId, int page, int pageSize)
        {
            var items = await _messageRepository.GetMessagesBetweenAsync(userId, otherUserId, page, pageSize);
            var total = await _messageRepository.CountMessagesBetweenAsync(userId, otherUserId);

            return new ConversationPagedDto
            {
                Items = items.OrderBy(m => m.DateEnvoi).Select(MapMessage).ToList(),
                Total = total
            };
        }

        public async Task<MessageDto> SendAsync(int senderId, SendMessageDto dto)
        {
            var contenu = dto.Contenu?.Trim() ?? string.Empty;
            var aPieceJointe = !string.IsNullOrWhiteSpace(dto.PieceJointeUrl);

            if (string.IsNullOrWhiteSpace(contenu) && !aPieceJointe)
                throw new ArgumentException("Le message ne peut pas être vide.");
            if (dto.DestinataireUserId <= 0 || dto.DestinataireUserId == senderId)
                throw new ArgumentException("Destinataire invalide.");

            var message = new MessagePrive
            {
                ExpediteurUserId = senderId,
                DestinataireUserId = dto.DestinataireUserId,
                Contenu = contenu,
                PieceJointeUrl = string.IsNullOrWhiteSpace(dto.PieceJointeUrl) ? null : dto.PieceJointeUrl.Trim(),
                PieceJointeNom = string.IsNullOrWhiteSpace(dto.PieceJointeNom) ? null : dto.PieceJointeNom.Trim(),
                PieceJointeType = string.IsNullOrWhiteSpace(dto.PieceJointeType) ? null : dto.PieceJointeType.Trim(),
                DateEnvoi = DateTime.UtcNow,
                Lu = false
            };

            await _messageRepository.AddAsync(message);
            await _messageRepository.SaveChangesAsync();

            var sauvegarde = await _messageRepository.GetByIdAsync(message.Id);
            return MapMessage(sauvegarde!);
        }

        public async Task<int> MarkConversationReadAsync(int userId, int otherUserId)
        {
            await _messageRepository.MarkConversationReadAsync(userId, otherUserId);
            await _messageRepository.SaveChangesAsync();
            return await _messageRepository.CountUnreadAsync(userId);
        }

        public async Task<int> CountUnreadAsync(int userId)
        {
            return await _messageRepository.CountUnreadAsync(userId);
        }

        private static string? ApercuDernierMessage(MessagePrive? m)
        {
            if (m == null) return null;
            if (!string.IsNullOrWhiteSpace(m.Contenu)) return m.Contenu;
            if (!string.IsNullOrWhiteSpace(m.PieceJointeNom)) return "📎 " + m.PieceJointeNom;
            return "📎 Pièce jointe";
        }

        private static MessageDto MapMessage(MessagePrive m) => new()
        {
            Id = m.Id,
            ExpediteurUserId = m.ExpediteurUserId,
            ExpediteurNom = m.Expediteur?.Username,
            DestinataireUserId = m.DestinataireUserId,
            DestinataireNom = m.Destinataire?.Username,
            Contenu = m.Contenu,
            PieceJointeUrl = m.PieceJointeUrl,
            PieceJointeNom = m.PieceJointeNom,
            PieceJointeType = m.PieceJointeType,
            DateEnvoi = m.DateEnvoi,
            Lu = m.Lu,
            DateLecture = m.DateLecture
        };
    }
}
