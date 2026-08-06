namespace Backend.BL.IBL
{
    public interface ICurrentUserService
    {
        int UserId { get; }
        string UserName { get; }
        bool IsAdmin { get; }
    }
}
