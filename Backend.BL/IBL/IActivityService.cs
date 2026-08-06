using Backend.DAL.Entities;
using Backend.DTO.Activity;

namespace Backend.BL.IBL
{
    public interface IActivityService
    {
        /// Enregistre un événement dans la timeline du client
        Task EnregistrerEvenementAsync(
            int clientId, string type, string message,
            int? utilisateurId = null, string? utilisateurNom = null,
            string? donneesJson = null, int? idLien = null, string? typeLien = null);

        Task<PagedEvenementsDto> GetEvenementsAsync(int clientId, int page, int pageSize);

        Task<NoteClientDto> CreateNoteAsync(CreateNoteClientDto dto);
        Task<NoteClientDto> UpdateNoteAsync(int id, UpdateNoteClientDto dto);
        Task<bool> DeleteNoteAsync(int id);
        Task<List<NoteClientDto>> GetNotesAsync(int clientId);

        Task<ContactClientDto> CreateContactAsync(CreateContactClientDto dto);
        Task<ContactClientDto> UpdateContactAsync(int id, UpdateContactClientDto dto);
        Task<bool> DeleteContactAsync(int id);
        Task<List<ContactClientDto>> GetContactsAsync(int clientId);
    }

    public class PagedEvenementsDto
    {
        public List<EvenementClientDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
