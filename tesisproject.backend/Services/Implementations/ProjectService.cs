using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _uow;

        public ProjectService(IUnitOfWork uow) => _uow = uow;

        // ================= READS (proyección a DTO en Service) =================

        public async Task<List<ProjectListResponseDTO>> GetAllAsync(CancellationToken ct = default)
        {
            return await _uow.Projects
                .Query() // AsNoTracking por defecto en el genérico
                .Select(p => new ProjectListResponseDTO(
                    p.ProjectId,
                    p.ProjectCode,
                    p.ProjectName,
                    p.ProjectState.Name,   // ajusta si el catálogo usa otra propiedad
                    p.ProjectType.Name,
                    p.ProjectGroup.Name,
                    p.StartDate,
                    p.TentativeEndDate,
                    p.ExecutionPercentage
                ))
                .ToListAsync(ct);
        }

        public async Task<ProjectListResponseDTO?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _uow.Projects
                .Query()
                .Where(p => p.ProjectId == id)
                .Select(p => new ProjectListResponseDTO(
                    p.ProjectId,
                    p.ProjectCode,
                    p.ProjectName,
                    p.ProjectState.Name,
                    p.ProjectType.Name,
                    p.ProjectGroup.Name,
                    p.StartDate,
                    p.TentativeEndDate,
                    p.ExecutionPercentage
                ))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<ProjectListResponseDTO>> GetByTypeAsync(int projectTypeId, CancellationToken ct = default)
        {
            // Puedes reutilizar el método específico del repo o proyectar desde Query()
            // Aquí uso Query() para evitar traer entidades completas si luego proyectas
            return await _uow.Projects
                .Query()
                .Where(p => p.ProjectTypeId == projectTypeId)
                .Select(p => new ProjectListResponseDTO(
                    p.ProjectId,
                    p.ProjectCode,
                    p.ProjectName,
                    p.ProjectState.Name,
                    p.ProjectType.Name,
                    p.ProjectGroup.Name,
                    p.StartDate,
                    p.TentativeEndDate,
                    p.ExecutionPercentage
                ))
                .ToListAsync(ct);
        }

        // ================= WRITES (usa el repo; retorna DTO) =================

        public async Task<ProjectListResponseDTO> CreateAsync(AddProjectRequestDTO dto, CancellationToken ct = default)
        {
            var entity = MapToEntity(dto);

            await _uow.Projects.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            // Si necesitas nombres de catálogos inmediatamente, re-proyecta tras persistir:
            var created = await _uow.Projects
                .Query()
                .Where(p => p.ProjectId == entity.ProjectId)
                .Select(p => new ProjectListResponseDTO(
                    p.ProjectId,
                    p.ProjectCode,
                    p.ProjectName,
                    p.ProjectState.Name,
                    p.ProjectType.Name,
                    p.ProjectGroup.Name,
                    p.StartDate,
                    p.TentativeEndDate,
                    p.ExecutionPercentage
                ))
                .FirstAsync(ct);

            return created;
        }

        public async Task<bool> UpdateAsync(int id, UpdateProjectRequestDTO dto, CancellationToken ct = default)
        {
            var current = await _uow.Projects.GetByIdAsync(new object[] { id }, ct);
            if (current is null) return false;

            ApplyUpdate(current, dto);
            _uow.Projects.Update(current);
            await _uow.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var current = await _uow.Projects.GetByIdAsync(new object[] { id }, ct);
            if (current is null) return false;

            _uow.Projects.Remove(current);
            await _uow.SaveChangesAsync(ct);
            return true;
        }

        // ================= MAPPING HELPERS =================

        private static Project MapToEntity(AddProjectRequestDTO dto)
        {
            return new Project
            {
                // ProjectId lo asigna la DB
                ProjectCode = dto.ProjectCode ?? string.Empty,
                CreatedByUserId = dto.CreatedByUserId,
                ProjectTypeId = dto.ProjectTypeId,
                ProjectStateId = dto.ProjectStateId,
                ProjectGroupId = dto.ProjectGroupId,
                SenesytGroupId = dto.SenesytGroupId,
                InitialDocumentId = dto.InitialDocumentId,
                ProjectName = dto.ProjectName,
                ProjectObjective = dto.ProjectObjective,
                ResearchLine = dto.ResearchLine,
                StartDate = dto.StartDate,
                TentativeEndDate = dto.TentativeEndDate,
                ExecutionPercentage = 0
            };
        }

        private static void ApplyUpdate(Project target, UpdateProjectRequestDTO dto)
        {
            // No tocar: target.ProjectId, target.CreatedByUserId, target.ProjectCode (si es autogenerado)

            target.ProjectName = dto.ProjectName;
            target.ProjectObjective = dto.ProjectObjective;
            target.ResearchLine = dto.ResearchLine;

            target.ProjectTypeId = dto.ProjectTypeId;
            target.ProjectStateId = dto.ProjectStateId;
            target.ProjectGroupId = dto.ProjectGroupId;
            target.SenesytGroupId = dto.SenesytGroupId;
            target.InitialDocumentId = dto.InitialDocumentId;

            target.StartDate = dto.StartDate;
            target.TentativeEndDate = dto.TentativeEndDate;
            target.RealEndDate = dto.RealEndDate;

            target.ExecutionPercentage = dto.ExecutionPercentage;
        }
    }
}
