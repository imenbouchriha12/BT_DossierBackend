using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IMessageRepository
    {
        Task<List<MessagePrive>> GetMessagesBetweenAsync(int userId, int otherUserId, int page, int pageSize);
        Task<int> CountMessagesBetweenAsync(int userId, int otherUserId);
        Task<List<MessagePrive>> GetAllForUserAsync(int userId);
        Task<int> CountUnreadAsync(int userId);
        Task<MessagePrive?> GetByIdAsync(int id);
        Task AddAsync(MessagePrive message);
        Task MarkConversationReadAsync(int userId, int otherUserId);
        Task SaveChangesAsync();
    }
}
