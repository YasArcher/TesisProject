using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Unified;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.DTOs.FacultyScope.Request;
using tesisproject.shared.DTOs.FacultyScope.Response;
using tesisproject.shared.Responses;

internal static class UnifiedRequestAdapterTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        using var cts = new CancellationTokenSource();
        var request = new AddProjectRequestDTO
        {
            ProjectName = "name", ProjectCode = "code", ProjectTypeId = 2, ProjectGroupId = 3,
            ResearchCategoryIds = [4, 5], ApprovalDate = new DateTime(2026, 1, 1), StartDate = new DateTime(2026, 2, 1),
            ProjectStateId = 6, DurationInMonths = 7, FacultyId = 501, ConvocationId = 8, ProjectOriginTypeId = 9
        };
        var errors = new Dictionary<string, string[]> { ["externalFacultyId"] = ["invalid external ID"], ["identity"] = ["mapping conflict"] };
        var project = new UnifiedProjectsController(Stub.For<IUnifiedProjectService>((method, args) =>
        {
            check(method.Name == "CreateAsync", "Project adapter targets CreateAsync");
            check((CancellationToken)args[1]! == cts.Token, "Project adapter CT");
            var adapted = (UnifiedAddProjectRequestDTO)args[0]!;
            foreach (var property in typeof(AddProjectRequestDTO).GetProperties())
            {
                var target = property.Name == "FacultyId" ? "ExternalFacultyId" : property.Name;
                check(Equals(property.GetValue(request), typeof(UnifiedAddProjectRequestDTO).GetProperty(target)!.GetValue(adapted)), "Project adapter field " + property.Name);
            }
            return Task.FromResult(ServiceResult<ProjectListResponseDTO>.Fail("mapping conflict", ErrorType.Conflict, "IDENTITY_MAPPING_CONFLICT", errors));
        }));
        var output = (ServiceResult<ProjectListResponseDTO>)((ConflictObjectResult)(await project.Create(request, cts.Token)).Result!).Value!;
        check(request.FacultyId == 501 && request.ProjectOriginTypeId == 9, "HTTP request is not mutated");
        check(output.ValidationErrors!.ContainsKey("FacultyId") && output.ValidationErrors.ContainsKey("identity"), "HTTP validation names retain all errors");
        check(errors.ContainsKey("externalFacultyId"), "Service error dictionary remains unchanged");
        check(output.ErrorCode == "IDENTITY_MAPPING_CONFLICT" && output.Message == "mapping conflict" && output.Error == ErrorType.Conflict, "Adapter preserves error metadata");

        var scopeService = Stub.For<IUnifiedFacultyScopeService>((method, args) =>
        {
            if (method.Name == "CreateAsync")
            {
                var dto = (UnifiedCreateFacultyScopeRequestDTO)args[0]!;
                check(dto.Name == "scope" && dto.ExternalFacultyIds!.SequenceEqual([501, 502]), "Scope create external IDs");
            }
            else
            {
                check(method.Name == "SetFacultiesAsync" && (int)args[0]! == 17, "Scope set target/local scope ID");
                check(((UnifiedSetFacultyScopeFacultiesRequestDTO)args[1]!).ExternalFacultyIds!.SequenceEqual([501, 502]), "Scope set external IDs");
            }
            check(args.OfType<CancellationToken>().Single() == cts.Token, "Scope adapter CT");
            return Task.FromResult(ServiceResult<FacultyScopeResponseDTO>.Fail("invalid", ErrorType.Validation, "ACADEMIC_REFERENCE_INVALID", new() { ["externalFacultyIds"] = ["invalid"] }));
        });
        var scopes = new UnifiedFacultyScopesController(scopeService, Stub.For<ICurrentUserService>((_, _) => throw new InvalidOperationException("No actor access required in adapter")));
        var create = await scopes.Create(new CreateFacultyScopeRequestDTO { Name = "scope", FacultyIds = [501, 502] }, cts.Token);
        var set = await scopes.SetFaculties(17, new SetFacultyScopeFacultiesRequestDTO { FacultyIds = [501, 502] }, cts.Token);
        foreach (var result in new[] { create, set })
            check(((ServiceResult<FacultyScopeResponseDTO>)((BadRequestObjectResult)result.Result!).Value!).ValidationErrors!.ContainsKey("FacultyIds"), "Scope HTTP validation field");
    }
}
