using Backend.DTO.Message;

namespace Backend.BL.IBL
{
    public interface IMessageService
    {
        Task<List<ConversationDto>> GetConversationsAsync(int userId);
        Task<ConversationPagedDto> GetMessagesAsync(int userId, int otherUserId, int page, int pageSize);
        Task<MessageDto> SendAsync(int senderId, SendMessageDto dto);
        Task<int> MarkConversationReadAsync(int userId, int otherUserId);
        Task<int> CountUnreadAsync(int userId);
    }
}
