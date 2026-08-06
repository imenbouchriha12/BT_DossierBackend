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
                .ForMember(dest => dest.Statut, opt => opt.MapFrom(src =>
                    src.Statut == StatutClient.EnAttente ? "En attente" : src.Statut.ToString()))
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

            // Finance (SISS, lecture seule)
            CreateMap<Compte, Backend.DTO.Finance.CompteDto>();
            CreateMap<Backend.DAL.Entities.CompteSearchItem, Backend.DTO.Finance.CompteDto>();
            CreateMap<Cours, Backend.DTO.Finance.CoursDto>();
            CreateMap<Mouvement, Backend.DTO.Finance.MouvementDto>();

            // Synchronisation
            CreateMap<SynchronisationLog, Backend.DTO.Synchronisation.SynchronisationResultDto>();
            CreateMap<SynchronisationRejet, Backend.DTO.Synchronisation.SynchronisationRejetDto>();

            // Workflow de validation multi-niveaux
            CreateMap<EtapeValidation, Backend.DTO.Workflow.EtapeValidationDto>();

            CreateMap<WorkflowValidation, Backend.DTO.Workflow.WorkflowValidationDto>()
                .ForMember(dest => dest.TypeDocument,
                    opt => opt.MapFrom(src => src.Document != null ? src.Document.TypeDocument : null))
                .ForMember(dest => dest.NomFichier,
                    opt => opt.MapFrom(src => src.Document != null ? src.Document.NomFichier : null))
                .ForMember(dest => dest.StatutValidation,
                    opt => opt.MapFrom(src => src.Document != null ? src.Document.StatutValidation : null))
                .ForMember(dest => dest.IdClient,
                    opt => opt.MapFrom(src => src.Document != null ? src.Document.IdClient : (int?)null))
                .ForMember(dest => dest.CodeClient,
                    opt => opt.MapFrom(src => src.Document != null && src.Document.Client != null
                        ? src.Document.Client.CodeClient : null))
                .ForMember(dest => dest.NomClient,
                    opt => opt.MapFrom(src => src.Document != null && src.Document.Client != null
                        ? $"{src.Document.Client.Prenom} {src.Document.Client.Nom}" : null))
                .ForMember(dest => dest.UploadedByNom,
                    opt => opt.MapFrom(src => src.Document != null && src.Document.UploadedBy != null
                        ? src.Document.UploadedBy.Username : null));

            // Activité client (timeline, notes, contacts)
            CreateMap<EvenementClient, Backend.DTO.Activity.EvenementClientDto>();
            CreateMap<NoteClient, Backend.DTO.Activity.NoteClientDto>();
            CreateMap<ContactClient, Backend.DTO.Activity.ContactClientDto>();
        }
    }
}
