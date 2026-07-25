using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Portal;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    public class DocumentTemplateService : IDocumentTemplateService
    {
        private readonly IDocumentTemplateRepository _templateRepo;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentTemplateService> _logger;

        public DocumentTemplateService(
            IDocumentTemplateRepository templateRepo,
            IMapper mapper,
            ILogger<DocumentTemplateService> logger)
        {
            _templateRepo = templateRepo;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<DocumentTemplateDto>> GetAllAsync()
        {
            var templates = await _templateRepo.GetAllAsync();
            return _mapper.Map<List<DocumentTemplateDto>>(templates);
        }

        public async Task<List<DocumentTemplateDto>> GetByTypeClientAsync(string typeClient)
        {
            var templates = await _templateRepo.GetByTypeClientAsync(typeClient);
            return _mapper.Map<List<DocumentTemplateDto>>(templates);
        }

        public async Task<DocumentTemplateDto?> GetByIdAsync(int id)
        {
            var template = await _templateRepo.GetByIdAsync(id);
            return template == null ? null : _mapper.Map<DocumentTemplateDto>(template);
        }

        public async Task<DocumentTemplateDto> CreateAsync(CreateDocumentTemplateDto dto)
        {
            _logger.LogInformation("Création template document : {TypeClient} / {NomDocument}", dto.TypeClient, dto.NomDocument);

            if (await _templateRepo.ExistsAsync(dto.TypeClient, dto.NomDocument))
                throw new InvalidOperationException($"Un template '{dto.NomDocument}' existe déjà pour le type '{dto.TypeClient}'.");

            var template = new DocumentTemplate
            {
                TypeClient      = dto.TypeClient.Trim(),
                NomDocument     = dto.NomDocument.Trim(),
                Description     = dto.Description?.Trim(),
                EstObligatoire  = dto.EstObligatoire,
                Ordre           = dto.Ordre,
                FormatsAcceptes = dto.FormatsAcceptes?.Trim().ToLowerInvariant(),
                TailleMaxMo     = dto.TailleMaxMo,
                IsActive        = true,
                DateCreation    = DateTime.UtcNow
            };

            await _templateRepo.AddAsync(template);
            await _templateRepo.SaveChangesAsync();

            _logger.LogInformation("Template créé avec Id={Id}", template.Id);
            return _mapper.Map<DocumentTemplateDto>(template);
        }

        public async Task<DocumentTemplateDto> UpdateAsync(int id, UpdateDocumentTemplateDto dto)
        {
            _logger.LogInformation("Mise à jour template {Id}", id);

            var template = await _templateRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Template introuvable (Id={id}).");

            if (dto.NomDocument != null)     template.NomDocument     = dto.NomDocument.Trim();
            if (dto.Description != null)     template.Description     = dto.Description.Trim();
            if (dto.EstObligatoire.HasValue) template.EstObligatoire  = dto.EstObligatoire.Value;
            if (dto.Ordre.HasValue)          template.Ordre           = dto.Ordre.Value;
            if (dto.FormatsAcceptes != null) template.FormatsAcceptes = dto.FormatsAcceptes.Trim().ToLowerInvariant();
            if (dto.TailleMaxMo.HasValue)    template.TailleMaxMo     = dto.TailleMaxMo.Value;
            if (dto.IsActive.HasValue)       template.IsActive        = dto.IsActive.Value;

            _templateRepo.Update(template);
            await _templateRepo.SaveChangesAsync();

            return _mapper.Map<DocumentTemplateDto>(template);
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Suppression template {Id}", id);

            var template = await _templateRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Template introuvable (Id={id}).");

            _templateRepo.Delete(template);
            await _templateRepo.SaveChangesAsync();
        }

        public async Task<List<string>> GetTypesClientsAsync()
        {
            var all = await _templateRepo.GetAllAsync();
            return all
                .Where(t => t.IsActive)
                .Select(t => t.TypeClient)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }
    }
}
