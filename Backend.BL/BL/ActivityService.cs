using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Activity;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    /// <summary>
    /// Gestion du fil d'activité (timeline), des notes internes et des contacts des clients.
    /// </summary>
    public class ActivityService : IActivityService
    {
        private readonly IActivityRepository _repository;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;
        private readonly ILogger<ActivityService> _logger;

        public ActivityService(
            IActivityRepository repository,
            ICurrentUserService currentUser,
            IMapper mapper,
            ILogger<ActivityService> logger)
        {
            _repository = repository;
            _currentUser = currentUser;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task EnregistrerEvenementAsync(
            int clientId, string type, string message,
            int? utilisateurId = null, string? utilisateurNom = null,
            string? donneesJson = null, int? idLien = null, string? typeLien = null)
        {
            try
            {
                var evenement = new EvenementClient
                {
                    IdClient = clientId,
                    Type = type,
                    Message = message,
                    DonneesJson = donneesJson,
                    UtilisateurId = utilisateurId ?? _currentUser.UserId,
                    UtilisateurNom = utilisateurNom ?? _currentUser.UserName,
                    DateEvenement = DateTime.UtcNow,
                    IdLien = idLien,
                    TypeLien = typeLien
                };

                await _repository.AddEvenementAsync(evenement);
                await _repository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Erreur lors de l'enregistrement d'un événement de timeline pour le client {ClientId}",
                    clientId);
            }
        }

        public async Task<PagedEvenementsDto> GetEvenementsAsync(int clientId, int page, int pageSize)
        {
            await VerifierClientAsync(clientId);

            var total = await _repository.CountEvenementsAsync(clientId);
            var items = await _repository.GetEvenementsAsync(clientId, page, pageSize);

            return new PagedEvenementsDto
            {
                Items = _mapper.Map<List<EvenementClientDto>>(items),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<NoteClientDto> CreateNoteAsync(CreateNoteClientDto dto)
        {
            await VerifierClientAsync(dto.IdClient);

            var note = new NoteClient
            {
                IdClient = dto.IdClient,
                Titre = dto.Titre,
                Contenu = dto.Contenu ?? string.Empty,
                Categorie = string.IsNullOrWhiteSpace(dto.Categorie) ? "Général" : dto.Categorie,
                EstEpinglee = dto.EstEpinglee,
                CreeParUserId = _currentUser.UserId,
                CreeParNom = _currentUser.UserName,
                DateCreation = DateTime.UtcNow
            };

            await _repository.AddNoteAsync(note);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Note créée pour le client {ClientId} par {User}",
                dto.IdClient, _currentUser.UserName);

            await EnregistrerEvenementAsync(
                dto.IdClient, EvenementTypes.Note,
                $"Note interne ajoutée{(string.IsNullOrWhiteSpace(note.Titre) ? "" : $" : {note.Titre}")}.",
                idLien: note.Id, typeLien: nameof(NoteClient));

            return _mapper.Map<NoteClientDto>(note);
        }

        public async Task<NoteClientDto> UpdateNoteAsync(int id, UpdateNoteClientDto dto)
        {
            var note = await _repository.GetNoteByIdAsync(id)
                ?? throw new KeyNotFoundException("Note introuvable.");

            if (dto.Titre != null) note.Titre = dto.Titre;
            if (dto.Contenu != null) note.Contenu = dto.Contenu;
            if (dto.Categorie != null) note.Categorie = dto.Categorie;
            if (dto.EstEpinglee.HasValue) note.EstEpinglee = dto.EstEpinglee.Value;

            note.ModifieParUserId = _currentUser.UserId;
            note.ModifieParNom = _currentUser.UserName;
            note.DateMaj = DateTime.UtcNow;

            await _repository.UpdateNoteAsync(note);
            await _repository.SaveChangesAsync();

            return _mapper.Map<NoteClientDto>(note);
        }

        public async Task<bool> DeleteNoteAsync(int id)
        {
            var note = await _repository.GetNoteByIdAsync(id);
            if (note == null) return false;

            await _repository.DeleteNoteAsync(note);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<List<NoteClientDto>> GetNotesAsync(int clientId)
        {
            await VerifierClientAsync(clientId);
            var notes = await _repository.GetNotesAsync(clientId);
            return _mapper.Map<List<NoteClientDto>>(notes);
        }

        public async Task<ContactClientDto> CreateContactAsync(CreateContactClientDto dto)
        {
            await VerifierClientAsync(dto.IdClient);

            var contact = new ContactClient
            {
                IdClient = dto.IdClient,
                TypeContact = string.IsNullOrWhiteSpace(dto.TypeContact) ? "Autre" : dto.TypeContact,
                Nom = dto.Nom ?? string.Empty,
                Prenom = dto.Prenom,
                Telephone = dto.Telephone,
                Email = dto.Email,
                Adresse = dto.Adresse,
                Relation = dto.Relation,
                DateCreation = DateTime.UtcNow
            };

            await _repository.AddContactAsync(contact);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Contact {Nom} ajouté au client {ClientId}",
                contact.Nom, dto.IdClient);

            await EnregistrerEvenementAsync(
                dto.IdClient, EvenementTypes.Contact,
                $"Contact « {contact.Prenom} {contact.Nom} » ({contact.TypeContact}) ajouté au dossier.",
                idLien: contact.Id, typeLien: nameof(ContactClient));

            return _mapper.Map<ContactClientDto>(contact);
        }

        public async Task<ContactClientDto> UpdateContactAsync(int id, UpdateContactClientDto dto)
        {
            var contact = await _repository.GetContactByIdAsync(id)
                ?? throw new KeyNotFoundException("Contact introuvable.");

            if (dto.TypeContact != null) contact.TypeContact = dto.TypeContact;
            if (dto.Nom != null) contact.Nom = dto.Nom;
            if (dto.Prenom != null) contact.Prenom = dto.Prenom;
            if (dto.Telephone != null) contact.Telephone = dto.Telephone;
            if (dto.Email != null) contact.Email = dto.Email;
            if (dto.Adresse != null) contact.Adresse = dto.Adresse;
            if (dto.Relation != null) contact.Relation = dto.Relation;

            await _repository.UpdateContactAsync(contact);
            await _repository.SaveChangesAsync();

            return _mapper.Map<ContactClientDto>(contact);
        }

        public async Task<bool> DeleteContactAsync(int id)
        {
            var contact = await _repository.GetContactByIdAsync(id);
            if (contact == null) return false;

            await _repository.DeleteContactAsync(contact);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<List<ContactClientDto>> GetContactsAsync(int clientId)
        {
            await VerifierClientAsync(clientId);
            var contacts = await _repository.GetContactsAsync(clientId);
            return _mapper.Map<List<ContactClientDto>>(contacts);
        }

        private async Task VerifierClientAsync(int clientId)
        {
            if (!await _repository.ClientExistsAsync(clientId))
                throw new KeyNotFoundException("Client introuvable.");
        }
    }
}
