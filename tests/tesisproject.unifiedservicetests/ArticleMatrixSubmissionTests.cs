using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.MassRegistration;

internal static class ArticleMatrixSubmissionTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        await using var provider = Services();
        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var context = sp.GetRequiredService<UnifiedDideDbContext>();
        var accessor = sp.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = ContextFor("owner");
        var title = new FieldCatalogEntry { FieldId = 1, FieldKey = "Title", EntityName = "Article", FieldLabel = "Titulo", IsRequired = true, IsVisible = true, IsActive = true };
        var doi = new FieldCatalogEntry { FieldId = 2, FieldKey = "Doi", EntityName = "Article", FieldLabel = "DOI", IsVisible = true, IsActive = true };
        context.FieldCatalogEntries.AddRange(title, doi);
        context.RegistrationMatrices.Add(Matrix(1, "Ready", title, doi));
        context.RegistrationMatrices.Add(Matrix(2, null, title, doi));
        await context.SaveChangesAsync();

        var service = sp.GetRequiredService<IUnifiedRegistrationMatrixService>();
        var submitted = await service.SubmitToStagingAsync(1, new SubmitRegistrationMatrixRequest(), "owner", false);
        check(submitted.Success && submitted.Data?.BatchResult?.Batch.Summary.TotalRows == 1 &&
            submitted.Data.BatchResult.Batch.Summary.ValidRows == 1 &&
            submitted.Data.BatchResult.InsertedRows == 0,
            "Submitting a matrix must stage one row without claiming final Article insertion.");
        var firstBatchId = submitted.Data!.BatchResult!.Batch.Summary.ImportBatchId;
        check(await context.ArticleStagingBatches.CountAsync() == 1 && await context.ArticleStagingRows.CountAsync() == 1 &&
            await context.Articles.CountAsync() == 0 &&
            (await context.RegistrationMatrices.FindAsync(1))?.LastImportBatchId is null,
            "Staging persistence must not write production articles or reuse the legacy import-batch reference.");
        var repeated = await service.SubmitToStagingAsync(1, new SubmitRegistrationMatrixRequest(), "owner", false);
        check(repeated.Success && repeated.Data?.BatchResult?.Batch.Summary.ImportBatchId == firstBatchId &&
            await context.ArticleStagingBatches.CountAsync() == 1,
            "Repeated submit must return the original batch without creating another.");
        var invalid = await service.SubmitToStagingAsync(2, new SubmitRegistrationMatrixRequest(), "owner", false);
        check(!invalid.Success && await context.ArticleStagingBatches.CountAsync() == 1 &&
            (await context.RegistrationMatrices.FindAsync(2))?.Status == "Draft",
            "Invalid rows must leave the matrix editable and create no batch.");
        var oversized = await service.UpdateCellAsync(2, 2,
            new UpdateRegistrationMatrixCellRequest { FieldId = 2, RawValue = new string('x', 21) }, "owner", false);
        check(!oversized.Success && oversized.ErrorCode == "ARTICLES_MATRIX_VALUE_TOO_LONG",
            "Server-side matrix editing must enforce the DOI length independently of the UI.");
        var unavailableWorkflow = await service.SubmitToStagingAsync(2,
            new SubmitRegistrationMatrixRequest { UseAuthorWorkflow = true }, "owner", false);
        check(!unavailableWorkflow.Success && unavailableWorkflow.ErrorCode == "ARTICLES_MATRIX_WORKFLOW_NOT_AVAILABLE",
            "Matrix staging must not silently claim that author workflow was started.");
        accessor.HttpContext = ContextFor("other");
        var privateBatch = await service.GetStagingSummaryAsync(1, "other", false);
        check(!privateBatch.Success && privateBatch.Data is null,
            "A different user must not see the owner's staging summary.");
    }

    private static RegistrationMatrix Matrix(int id, string? titleValue, FieldCatalogEntry title, FieldCatalogEntry doi)
    {
        var matrix = new RegistrationMatrix { RegistrationMatrixId = id, CreatedByUserId = "owner", Name = "Matrix " + id };
        matrix.Columns.Add(new RegistrationMatrixColumn { FieldId = 1, Field = title, DisplayOrder = 1 });
        matrix.Columns.Add(new RegistrationMatrixColumn { FieldId = 2, Field = doi, DisplayOrder = 2 });
        var row = new RegistrationMatrixRow { RegistrationMatrixRowId = id, RowNumber = 1 };
        if (titleValue is not null) row.Cells.Add(new RegistrationMatrixCell { FieldId = 1, RawValue = titleValue });
        row.Cells.Add(new RegistrationMatrixCell { FieldId = 2, RawValue = "10.1000/" + id });
        matrix.Rows.Add(row);
        return matrix;
    }

    private static DefaultHttpContext ContextFor(string owner) => new()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner)], "Test"))
    };

    private static ServiceProvider Services()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ExternalApis:BaseUrl"] = "https://example.invalid/",
            ["Storage:RootPath"] = Path.GetFullPath("artifacts/matrix-staging/storage")
        }).Build();
        var services = new ServiceCollection();
        services.AddUnifiedDide(config, options => options.UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        return services.BuildServiceProvider();
    }
}
