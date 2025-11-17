// Mapping/ProjectMapping.cs
using AutoMapper;

namespace tesisproject.backend.Mapping
{
    public class ProjectMapping : Profile
    {
        public ProjectMapping()
        {
            // Perfil intencionalmente vacío para evitar conflictos con DTOs de Project.
            // Cuando definamos exactamente tus DTOs de Project (nombres/campos),
            // agregamos aquí los CreateMap<...> correctos.
        }
    }
}
