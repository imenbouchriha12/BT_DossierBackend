using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.IDAL;
using Backend.DTO.Finance;

namespace Backend.BL.BL
{
    public class CoursService : ICoursService
    {
        private readonly ICoursRepository _coursRepository;
        private readonly IMapper _mapper;

        public CoursService(ICoursRepository coursRepository, IMapper mapper)
        {
            _coursRepository = coursRepository;
            _mapper = mapper;
        }

        public async Task<List<CoursDto>> GetLatestAsync(string? devise)
        {
            var cours = await _coursRepository.GetLatestAsync(devise);
            return _mapper.Map<List<CoursDto>>(cours);
        }

        public async Task<List<CoursDto>> GetHistoryAsync(string devise, DateTime? dateDebut, DateTime? dateFin)
        {
            var cours = await _coursRepository.GetHistoryAsync(devise, dateDebut, dateFin);
            return _mapper.Map<List<CoursDto>>(cours);
        }
    }
}
