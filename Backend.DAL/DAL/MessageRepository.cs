using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _context;

        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<MessagePrive>> GetMessagesBetweenAsync(int userId, int otherUserId, int page, int pageSize)
        {
            return await _context.MessagesPrives
                .Where(m => (m.ExpediteurUserId == userId && m.DestinataireUserId == otherUserId) ||
                            (m.ExpediteurUserId == otherUserId && m.DestinataireUserId == userId))
                .Include(m => m.Expediteur)
                .Include(m => m.Destinataire)
                .OrderByDescending(m => m.DateEnvoi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountMessagesBetweenAsync(int userId, int otherUserId)
        {
            return await _context.MessagesPrives
                .CountAsync(m => (m.ExpediteurUserId == userId && m.DestinataireUserId == otherUserId) ||
                                 (m.ExpediteurUserId == otherUserId && m.DestinataireUserId == userId));
        }

        public async Task<List<MessagePrive>> GetAllForUserAsync(int userId)
        {
            return await _context.MessagesPrives
                .Include(m => m.Expediteur)
                .Include(m => m.Destinataire)
                .Where(m => m.ExpediteurUserId == userId || m.DestinataireUserId == userId)
                .OrderByDescending(m => m.DateEnvoi)
                .ToListAsync();
        }

        public async Task<int> CountUnreadAsync(int userId)
        {
            return await _context.MessagesPrives
                .CountAsync(m => m.DestinataireUserId == userId && !m.Lu);
        }

        public async Task<MessagePrive?> GetByIdAsync(int id)
        {
            return await _context.MessagesPrives
                .Include(m => m.Expediteur)
                .Include(m => m.Destinataire)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task AddAsync(MessagePrive message)
        {
            await _context.MessagesPrives.AddAsync(message);
        }

        public async Task MarkConversationReadAsync(int userId, int otherUserId)
        {
            var nonLus = await _context.MessagesPrives
                .Where(m => m.DestinataireUserId == userId && m.ExpediteurUserId == otherUserId && !m.Lu)
                .ToListAsync();

            foreach (var m in nonLus)
            {
                m.Lu = true;
                m.DateLecture = DateTime.UtcNow;
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
