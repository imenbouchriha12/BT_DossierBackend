using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.IDAL;
using Backend.DTO.Client;
using Backend.DTO.Finance;

namespace Backend.BL.BL
{
    public class CompteService : ICompteService
    {
        private readonly ICompteRepository _compteRepository;
        private readonly IMapper _mapper;

        public CompteService(ICompteRepository compteRepository, IMapper mapper)
        {
            _compteRepository = compteRepository;
            _mapper = mapper;
        }

        public async Task<List<CompteDto>> GetByClientIdAsync(int idClient)
        {
            var comptes = await _compteRepository.GetByClientIdAsync(idClient);
            return _mapper.Map<List<CompteDto>>(comptes);
        }

        public async Task<CompteDto?> GetByIdAsync(int id)
        {
            var compte = await _compteRepository.GetByIdAsync(id);
            return compte == null ? null : _mapper.Map<CompteDto>(compte);
        }

        public async Task<List<CompteTotalDto>> GetTotauxAsync(int idClient)
        {
            var comptes = await _compteRepository.GetByClientIdAsync(idClient);
            return comptes
                .GroupBy(c => c.Devise)
                .Select(g => new CompteTotalDto
                {
                    Devise = g.Key,
                    Total = g.Sum(c => c.Solde),
                    TotalContreValeur = g.Sum(c => c.SoldeContreValeur),
                    NombreComptes = g.Count()
                })
                .OrderBy(t => t.Devise)
                .ToList();
        }

        public async Task<PagedResult<CompteDto>> SearchAsync(string? numCompte, string? agence, string? devise, string? codeClient, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var (items, total) = await _compteRepository.SearchAsync(numCompte, agence, devise, codeClient, page, pageSize);

            return new PagedResult<CompteDto>
            {
                Items = _mapper.Map<List<CompteDto>>(items),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
