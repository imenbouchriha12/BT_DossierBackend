using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class ActivityRepository : IActivityRepository
    {
        private readonly AppDbContext _context;

        public ActivityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<EvenementClient>> GetEvenementsAsync(int clientId, int page, int pageSize)
        {
            return await _context.EvenementClients
                .Where(e => e.IdClient == clientId)
                .OrderByDescending(e => e.DateEvenement)
                .ThenByDescending(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountEvenementsAsync(int clientId)
        {
            return await _context.EvenementClients
                .Where(e => e.IdClient == clientId)
                .CountAsync();
        }

        public async Task AddEvenementAsync(EvenementClient evenement)
        {
            await _context.EvenementClients.AddAsync(evenement);
        }

        public async Task<List<NoteClient>> GetNotesAsync(int clientId)
        {
            return await _context.NoteClients
                .Where(n => n.IdClient == clientId)
                .OrderByDescending(n => n.EstEpinglee)
                .ThenByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<NoteClient?> GetNoteByIdAsync(int id)
        {
            return await _context.NoteClients
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task AddNoteAsync(NoteClient note)
        {
            await _context.NoteClients.AddAsync(note);
        }

        public Task UpdateNoteAsync(NoteClient note)
        {
            _context.NoteClients.Update(note);
            return Task.CompletedTask;
        }

        public Task DeleteNoteAsync(NoteClient note)
        {
            _context.NoteClients.Remove(note);
            return Task.CompletedTask;
        }

        public async Task<List<ContactClient>> GetContactsAsync(int clientId)
        {
            return await _context.ContactClients
                .Where(c => c.IdClient == clientId)
                .OrderBy(c => c.TypeContact)
                .ThenBy(c => c.Nom)
                .ToListAsync();
        }

        public async Task<ContactClient?> GetContactByIdAsync(int id)
        {
            return await _context.ContactClients
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddContactAsync(ContactClient contact)
        {
            await _context.ContactClients.AddAsync(contact);
        }

        public Task UpdateContactAsync(ContactClient contact)
        {
            _context.ContactClients.Update(contact);
            return Task.CompletedTask;
        }

        public Task DeleteContactAsync(ContactClient contact)
        {
            _context.ContactClients.Remove(contact);
            return Task.CompletedTask;
        }

        public async Task<bool> ClientExistsAsync(int clientId)
        {
            return await _context.Clients.AnyAsync(c => c.Id == clientId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
