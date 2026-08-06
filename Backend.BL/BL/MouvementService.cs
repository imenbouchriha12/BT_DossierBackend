using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.IDAL;
using Backend.DTO.Client;
using Backend.DTO.Finance;

namespace Backend.BL.BL
{
    public class MouvementService : IMouvementService
    {
        private readonly IMouvementRepository _mouvementRepository;
        private readonly ICompteRepository _compteRepository;
        private readonly IMapper _mapper;

        public MouvementService(IMouvementRepository mouvementRepository, ICompteRepository compteRepository, IMapper mapper)
        {
            _mouvementRepository = mouvementRepository;
            _compteRepository = compteRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<MouvementDto>> GetByCompteAsync(string numCompte, string agence, DateTime? dateDebut, DateTime? dateFin, string? sens, decimal? montantMin, decimal? montantMax, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var (items, total) = await _mouvementRepository.GetByCompteAsync(numCompte, agence, dateDebut, dateFin, sens, montantMin, montantMax, page, pageSize);

            return new PagedResult<MouvementDto>
            {
                Items = _mapper.Map<List<MouvementDto>>(items),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<MouvementDto>> GetByClientAsync(int idClient, DateTime? dateDebut, DateTime? dateFin, string? sens, decimal? montantMin, decimal? montantMax, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var (items, total) = await _mouvementRepository.GetByClientAsync(idClient, dateDebut, dateFin, sens, montantMin, montantMax, page, pageSize);

            return new PagedResult<MouvementDto>
            {
                Items = _mapper.Map<List<MouvementDto>>(items),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
