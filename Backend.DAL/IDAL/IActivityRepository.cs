using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IActivityRepository
    {
        Task<List<EvenementClient>> GetEvenementsAsync(int clientId, int page, int pageSize);
        Task<int> CountEvenementsAsync(int clientId);
        Task AddEvenementAsync(EvenementClient evenement);

        Task<List<NoteClient>> GetNotesAsync(int clientId);
        Task<NoteClient?> GetNoteByIdAsync(int id);
        Task AddNoteAsync(NoteClient note);
        Task UpdateNoteAsync(NoteClient note);
        Task DeleteNoteAsync(NoteClient note);

        Task<List<ContactClient>> GetContactsAsync(int clientId);
        Task<ContactClient?> GetContactByIdAsync(int id);
        Task AddContactAsync(ContactClient contact);
        Task UpdateContactAsync(ContactClient contact);
        Task DeleteContactAsync(ContactClient contact);

        Task<bool> ClientExistsAsync(int clientId);
        Task SaveChangesAsync();
    }
}
