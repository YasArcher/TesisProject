using tesisproject.backend.Data.Entities;
using tesisproject.shared.DTOs.Articles;
using static System.Runtime.InteropServices.JavaScript.JSType;
using AutoMapper;
namespace tesisproject.backend.Mapping
{
    public class ArticleMapping : Profile
    {
        public ArticleMapping()
        {
            CreateMap<ArticleParticipant, ArticleParticipantDto>();
            CreateMap<Article, ArticleDto>()
                .ForMember(d => d.Participantes, cfg => cfg.MapFrom(s => s.Participantes.OrderBy(p => p.Index)));

            CreateMap<ArticleParticipantRequest, ArticleParticipant>();

            CreateMap<CreateArticleRequest, Article>()
                .ForMember(d => d.Participantes, cfg => cfg.MapFrom(s => s.Participantes));

            CreateMap<UpdateArticleRequest, Article>()
                .ForMember(d => d.Participantes, cfg => cfg.Ignore()); // se reemplazan manualmente en Update
        }
    }
}
