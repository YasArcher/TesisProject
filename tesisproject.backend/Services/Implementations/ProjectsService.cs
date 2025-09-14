using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Mapping;
using tesisproject.shared.Abstractions;
using tesisproject.shared.Abstractions.Project;
using tesisproject.shared.DTOs.Project;

namespace tesisproject.backend.Services;

public class ProjectsService : IProjectsService
{
    private readonly AppDbContext _db;
    public ProjectsService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _db.Projects.AsNoTracking().Select(p => p.ToDto()).ToListAsync(ct);
        return Result<IReadOnlyList<ProjectDto>>.Ok(items);
    }

    public async Task<Result<ProjectDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Projects.FindAsync([id], ct);
        return entity is null
            ? Result<ProjectDto>.Fail("Not found")
            : Result<ProjectDto>.Ok(entity.ToDto());
    }

    public async Task<Result<int>> CreateAsync(CreateProjectRequest req, CancellationToken ct = default)
    {
        if (await _db.Projects.AnyAsync(p => p.Code == req.Code, ct))
            return Result<int>.Fail("Code already exists");

        var entity = new Project { Code = req.Code, Name = req.Name };
        _db.Projects.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(entity.Id);
    }

    public async Task<Result> UpdateAsync(UpdateProjectRequest req, CancellationToken ct = default)
    {
        var entity = await _db.Projects.FindAsync([req.Id], ct);
        if (entity is null) return Result.Fail("Not found");
        entity.Code = req.Code;
        entity.Name = req.Name;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Projects.FindAsync([id], ct);
        if (entity is null) return Result.Fail("Not found");
        _db.Projects.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
