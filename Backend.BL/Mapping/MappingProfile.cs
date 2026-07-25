using AutoMapper;
using Backend.DAL.Entities;
using Backend.DTO.Auth;
using Backend.DTO.Client;
using Backend.DTO.Historique;
using Backend.DTO.Notification;
using Backend.DTO.Portal;

namespace Backend.BL.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src =>
                    src.UserRoles.Select(ur => ur.Role.Name).ToList()));

            CreateMap<Role, RoleDto>();

            CreateMap<Client, ClientDto>()
                .ForMember(dest => dest.Detail, opt => opt.MapFrom(src => src.Detail));

            CreateMap<DetailClient, DetailClientDto>();

            CreateMap<DocumentClient, DocumentDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CodeClient, opt => opt.MapFrom(src => src.Client != null ? src.Client.CodeClient : null))
                .ForMember(dest => dest.NomClient, opt => opt.MapFrom(src => src.Client != null ? $"{src.Client.Prenom} {src.Client.Nom}" : null));

            CreateMap<HistoriqueModification, HistoriqueDto>();

            CreateMap<Notification, NotificationDto>();

            // Portal mappings
            CreateMap<DocumentTemplate, Backend.DTO.Portal.DocumentTemplateDto>();
        }
    }
}
