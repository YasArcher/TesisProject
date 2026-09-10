using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Controllers.Unified;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.shared.DTOs.Configuration;

var name = "tesis_article_configuration_test_" + Guid.NewGuid().ToString("N");
var server = Environment.GetEnvironmentVariable("ARTICLES_TEST_SQL_SERVER") ?? @".\DINNOVA";
var services = new ServiceCollection();
services.AddUnifiedDide(new ConfigurationBuilder().Build(), o => o.UseSqlServer(
    $@"Server={server};Database={name};Integrated Security=True;TrustServerCertificate=True",
    sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")).AddInterceptors(new FailFormSave()));
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
var service = scope.ServiceProvider.GetRequiredService<IUnifiedArticleConfigurationService>();
var ct = CancellationToken.None;
int checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    checks++; Console.WriteLine("PASS: " + description);
}
try
{
    await db.Database.MigrateAsync();
    var form = await service.CreateForm(new() { FormKey = "ArticleForm", FormName = "Article", EntityName = "Article" }, ct);
    Check(form.Success, "Create form");
    int formId = form.Data!.FormId;
    Check((await service.GetForms("Article", ct)).Data!.Single().FormId == formId, "Read forms");
    var updated = await service.UpdateForm(formId, new() { FormName = "Renamed", IsActive = true }, ct);
    Check(updated.Success && updated.Data!.FormName == "Renamed", "Update form");
    var duplicate = await service.CreateForm(new() { FormKey = "ArticleForm", FormName = "Duplicate", EntityName = "Article" }, ct);
    Check(!duplicate.Success && duplicate.ErrorCode == "CONFIG_FORM_KEY_DUPLICATED", "Duplicate form rejected");
    var field = await service.CreateDynamicField(new() { EntityName = "Article", FieldKey = "Additional Note", FieldLabel = "Additional note" }, ct);
    Check(field.Success && field.Data!.FieldKey == "additional_note", "Create field with existing key normalization");
    int fieldId = field.Data!.FieldId;
    Check((await service.GetDynamicFields("Article", ct)).Data!.Single().FieldId == fieldId, "Read dynamic fields");
    var updateField = await service.UpdateField(fieldId, new() { FieldLabel = "Note", IsActive = true, IsEditable = true, IsVisible = true, MaxLength = 200 }, ct);
    Check(updateField.Success && updateField.Data!.MaxLength == 200, "Update field");
    var option = await service.CreateFieldOption(fieldId, new() { OptionLabel = "Option one", OptionValue = "Option One", IsActive = true }, ct);
    Check(option.Success && option.Data!.OptionValue == "option_one", "Create option");
    int optionId = option.Data!.DynamicFieldOptionId;
    Check((await service.UpdateFieldOption(fieldId, optionId, new() { OptionLabel = "Updated option", OptionValue = "Option One", IsActive = true }, ct)).Success
        && (await service.GetFieldOptions(fieldId, ct)).Data!.Single().OptionLabel == "Updated option", "Update and read option");
    var assignment = await service.AddFieldToForm(formId, new() { FieldId = fieldId, GroupName = "Details", ColumnSpan = 9 }, ct);
    Check(assignment.Success && assignment.Data!.ColumnSpan == 2, "Assign field to form and clamp columns");
    int assignmentId = assignment.Data!.FormFieldId;
    Check(!(await service.AddFieldToForm(formId, new() { FieldId = fieldId }, ct)).Success, "Duplicate assignment rejected");
    Check((await service.UpdateFormField(formId, assignmentId, new() { IsVisible = true, IsEditable = true, IsRequired = true, GroupName = "Details", DisplayOrder = 1 }, ct)).Success,
        "Update assignment");
    var resolved = await service.GetResolvedActiveForm("Article", "Article Form", ct);
    Check(resolved.Success && resolved.Data!.FormId == formId && resolved.Data.Sections.Single().Fields.Single().Options.Single().OptionLabel == "Updated option"
        && resolved.Data.Sections.Single().Fields.Single().IsRequired, "Resolved form includes sections, field rules and options via effective selector");
    Check((await service.GetResolvedForm("ArticleForm", ct)).Data!.FormId == formId, "Explicit by-key form preview preserved");
    Check(!(await service.DeleteField(fieldId, ct)).Success, "Assigned field cannot be deleted");

    var second = await service.CreateForm(new() { FormKey = "Second", FormName = "Second", EntityName = "Article" }, ct);
    Check(second.Success && (await service.GetForms("Article", ct)).Data!.Count(f => f.IsActive) == 1
        && (await service.GetResolvedActiveForm("Article", null, ct)).Data!.FormId == second.Data!.FormId, "Activation deactivates previous form atomically");
    // Same scope exercises tracker consistency after ExecuteUpdate.
    Check((await service.UpdateForm(formId, new() { FormName = "Renamed", IsActive = true }, ct)).Success
        && (await service.GetForms("Article", ct)).Data!.Single(f => f.IsActive).FormId == formId,
        "Reactivation remains consistent with previously tracked forms");
    try { await service.CreateForm(new() { FormKey = "fail", FormName = "FORCE_CONFIG_ROLLBACK", EntityName = "Article" }, ct); throw new Exception("Expected injected failure"); }
    catch (InvalidOperationException e) when (e.Message == "Injected configuration save failure") { }
    Check((await service.GetForms("Article", ct)).Data!.Single(f => f.IsActive).FormId == formId, "Failed save rolls back bulk deactivation");

    // Multiple active candidates may predate this service. The existing selector owns their ordering.
    var exact = await db.Set<FormDefinition>().SingleAsync(f => f.FormId == formId);
    var normalized = new FormDefinition { FormKey = "Article_Form", FormName = "Normalized", EntityName = "Article", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(1) };
    var fallback = new FormDefinition { FormKey = "Fallback", FormName = "Fallback", EntityName = "Article", IsActive = true, CreatedAt = normalized.CreatedAt };
    db.AddRange(normalized, fallback); await db.SaveChangesAsync();
    Check((await service.GetResolvedActiveForm("Article", "ArticleForm", ct)).Data!.FormId == formId
        && (await service.GetResolvedActiveForm("Article", "article form", ct)).Data!.FormId == normalized.FormId
        && (await service.GetResolvedActiveForm("Article", "unknown", ct)).Data!.FormId == fallback.FormId,
        "Effective selector: exact, normalized, timestamp then ID tie-break");

    foreach (var alias in new[] { "Title", "Authors", "Journal", "IndexingDatabase", "Sjr", "Quartile", "IssnIsbn", "Doi", "Year", "ConsultationUrl", "CUARTIL" })
    {
        var rejected = await service.CreateDynamicField(new() { EntityName = "Article", FieldKey = alias, FieldLabel = alias }, ct);
        Check(!rejected.Success && rejected.ErrorCode == "CONFIG_BASE_ATTRIBUTE_DUPLICATED", "Reject duplicate canonical attribute: " + alias);
    }
    Check(!(await service.UpdateField(fieldId, new() { FieldLabel = "DOI", IsActive = true }, ct)).Success, "Renaming a dynamic field cannot bypass canonical guard");
    await db.Database.ExecuteSqlRawAsync("""
        SET IDENTITY_INSERT dbo.ProductTypes ON;
        INSERT dbo.ProductTypes(Id,Name,IsActive,IsLocked) VALUES(1,N'Scientific',1,0),(2,N'Regional',1,0);
        SET IDENTITY_INSERT dbo.ProductTypes OFF;
        SET IDENTITY_INSERT dbo.ProductAttributes ON;
        INSERT dbo.ProductAttributes(Id,Name,IsActive,IsLocked,DataType) VALUES(8,N'DOI',1,0,0);
        SET IDENTITY_INSERT dbo.ProductAttributes OFF;
        INSERT dbo.ProductAttributeDefinitions(ProductTypeId,ProductAttributeId,IsRequired,DisplayOrder) VALUES(1,8,0,1),(2,8,0,1);
        """);
    var doiField = new FieldCatalogEntry { FieldKey = "Doi", EntityName = "Article", FieldLabel = "DOI", SourceType = "Physical", DataType = "string",
        PhysicalTableName = "dbo.Articles", PhysicalColumnName = "Doi", IsActive = true, IsVisible = true, IsEditable = true, IsSystemField = true, CreatedAt = DateTime.UtcNow };
    db.Add(doiField); await db.SaveChangesAsync();
    var doiAssignment = await service.AddFieldToForm(formId, new() { FieldId = doiField.FieldId }, ct);
    Check(doiAssignment.Success, "Existing base descriptor validated against real ProductAttributeDefinitions");
    var withDoi = await service.GetResolvedForm("ArticleForm", ct);
    var resolvedDoi = withDoi.Data!.Sections.SelectMany(s => s.Fields).Single(f => f.FieldId == doiField.FieldId);
    Check(!resolvedDoi.IsDynamic && resolvedDoi.SourceType == "ProductValue" && resolvedDoi.PhysicalTableName == "dbo.ArticleReadView" && resolvedDoi.PhysicalColumnName == "Doi",
        "Resolved base descriptor points to canonical read source, never Article.Doi");
    Check(!await db.Set<ProductValue>().AnyAsync() && !await db.Set<DynamicFieldValue>().AnyAsync(), "Configuration does not write product or dynamic values");
    await db.Database.ExecuteSqlRawAsync("DELETE dbo.ProductAttributeDefinitions WHERE ProductTypeId=2;");
    Check(!(await service.GetResolvedForm("ArticleForm", ct)).Success, "Missing canonical definition is explicit, without dynamic fallback");

    var faculty = new Faculty { Name = "Faculty", Acronym = "FAC" };
    db.Add(faculty); await db.SaveChangesAsync();
    var admin = await service.GetAdminCatalog("faculties", ct);
    Check(admin.Success && admin.Data!.Single().Id == faculty.FacultyId && admin.Data.Single().Code == "FAC", "Administrative catalog uses Unified Faculty ID and acronym");
    var catalogField = new FieldCatalogEntry { FieldKey = "FacultyId", EntityName = "Article", FieldLabel = "Faculty", SourceType = "Physical", DataType = "int", ReferenceTableName = "faculties" };
    db.Add(catalogField); await db.SaveChangesAsync();
    Check((await service.GetCatalogItems(catalogField.FieldId, null, ct)).Data!.Single().Id == faculty.FacultyId, "Field catalog lookup");
    Check((await service.DeleteFieldOption(fieldId, optionId, ct)).Success && (await service.GetFieldOptions(fieldId, ct)).Data!.Count == 0, "Delete option");
    Check((await service.DeleteFormField(formId, assignmentId, ct)).Success, "Remove field/form assignment");
    Check((await service.DeleteField(fieldId, ct)).Success && !(await service.GetFields("Article", ct)).Data!.Any(f => f.FieldId == fieldId), "Delete unreferenced field");
    Check((await service.DeleteForm(formId, ct)).Success && !(await service.GetForms("Article", ct)).Data!.Any(f => f.FormId == formId), "Delete form and its remaining assignments");

    var feature = new ControllerFeature();
    new ControllerFeatureProvider().PopulateFeature([new AssemblyPart(typeof(UnifiedArticlesConfigurationController).Assembly)], feature);
    Check(!feature.Controllers.Any(t => t.Name is "ArticlesConfigurationController" or "ArticlesCatalogsController")
        && feature.Controllers.Contains(typeof(UnifiedArticlesConfigurationController).GetTypeInfo()) && feature.Controllers.Contains(typeof(UnifiedArticlesCatalogsController).GetTypeInfo()),
        "Unified controllers active; legacy controllers inactive");
    foreach (var type in new[] { typeof(UnifiedArticlesConfigurationController), typeof(UnifiedArticlesCatalogsController), typeof(UnifiedArticleConfigurationService) })
        Check(!type.GetConstructors().SelectMany(c => c.GetParameters()).Any(p => typeof(DbContext).IsAssignableFrom(p.ParameterType)), "No direct DbContext dependency: " + type.Name);
    Check(ControllerContractSnapshot.Matches(typeof(UnifiedArticlesConfigurationController)) && ControllerContractSnapshot.Matches(typeof(UnifiedArticlesCatalogsController)), "DTO/binding/routes/auth snapshot preserved");
    Console.WriteLine($"PASS: {checks} Article configuration SQL/EF assertions.");
}
finally
{
    if (!name.StartsWith("tesis_article_configuration_test_", StringComparison.Ordinal)) throw new Exception("Invalid test database");
    await db.Database.EnsureDeletedAsync();
}

sealed class FailFormSave : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context!.ChangeTracker.Entries<FormDefinition>().Any(e => e.State == EntityState.Added && e.Entity.FormName == "FORCE_CONFIG_ROLLBACK"))
            throw new InvalidOperationException("Injected configuration save failure");
        return ValueTask.FromResult(result);
    }
}
