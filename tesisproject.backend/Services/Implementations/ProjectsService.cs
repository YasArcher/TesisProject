using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;   // Project
using tesisproject.shared.Abstractions;     // Result
using tesisproject.shared.Abstractions.Project;
using tesisproject.shared.DTOs.Project;     // ProjectDto, CreateProjectRequest, UpdateProjectRequest

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectsService : IProjectsService
    {
        private readonly AppDbContext _db;

        public ProjectsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(CancellationToken ct = default)
        {
            // 1) Traemos entidades
            var entities = await _db.Projects
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            // 2) Construimos DTOs FUERA del LINQ de EF y con argumentos CON NOMBRE
            var list = entities.Select(p => new ProjectDto(
                Id: p.Id,
                Code: p.Code,
                Name: p.Name,
                CreatedAt: p.CreatedAt
            )).ToList();

            return Result<IReadOnlyList<ProjectDto>>.Ok(list);
        }

        public async Task<Result<ProjectDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var p = await _db.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (p is null) return Result<ProjectDto>.Fail("Not found");

            var dto = new ProjectDto(
                Id: p.Id,
                Code: p.Code,
                Name: p.Name,
                CreatedAt: p.CreatedAt
            );

            return Result<ProjectDto>.Ok(dto);
        }

        public async Task<Result<int>> CreateAsync(CreateProjectRequest req, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
                return Result<int>.Fail("Code and Name are required.");

            var exists = await _db.Projects.AnyAsync(p => p.Code == req.Code, ct);
            if (exists) return Result<int>.Fail("Code already exists.");

            var e = new Project
            {
                Code = req.Code.Trim(),
                Name = req.Name.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Projects.Add(e);
            await _db.SaveChangesAsync(ct);
            return Result<int>.Ok(e.Id);
        }

        public async Task<Result> UpdateAsync(UpdateProjectRequest req, CancellationToken ct = default)
        {
            var e = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.Id, ct);
            if (e is null) return Result.Fail("Not found");

            if (!string.IsNullOrWhiteSpace(req.Code) && !req.Code.Equals(e.Code, StringComparison.Ordinal))
            {
                var codeTaken = await _db.Projects.AnyAsync(p => p.Code == req.Code && p.Id != req.Id, ct);
                if (codeTaken) return Result.Fail("Code already exists.");
                e.Code = req.Code.Trim();
            }

            if (!string.IsNullOrWhiteSpace(req.Name))
                e.Name = req.Name.Trim();

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
        {
            var e = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (e is null) return Result.Fail("Not found");
            _db.Projects.Remove(e);
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
