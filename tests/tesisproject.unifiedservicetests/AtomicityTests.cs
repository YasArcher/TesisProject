using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging.Abstractions;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Base;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Data.UnifiedEntities.Export;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Products.ProductTypeDesign.Request;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

internal static class AtomicityTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        {
            var f = new AtomicFixture();
            var result = await f.Design.SaveDesignAsync(new()
            {
                ProductType = new() { Name = " New type ", IsActive = true },
                Attributes = [new() { Name = "A", IsActive = true }, new() { Name = "B", IsActive = true }],
                Definitions = [new() { ProductAttributeId = 0, IsRequired = true, DisplayOrder = 3 }]
            });
            check(result.Success && f.Saves == 1, "Design aggregate commits exactly once.");
            check(f.Types.Single().Name == "New type" && f.Attributes.Count == 2, "Design creates all catalog rows.");
            var definition = f.Definitions.Single();
            check(definition.ProductTypeId > 0 && definition.ProductAttributeId == f.Attributes.Last().Id,
                "Generated FK references and last Id=0 mapping are preserved.");
        }
        foreach (var failure in new[] { "blank", "locked", "missing", "duplicate", "definitions" })
        {
            var f = new AtomicFixture();
            f.Types.Add(new() { Id = 10, Name = "Original", IsActive = true });
            f.Attributes.Add(new() { Id = 20, Name = "Original attribute", IsActive = true });
            f.Attributes.Add(new() { Id = 21, Name = "Locked", IsLocked = true });
            f.Definitions.Add(new() { Id = 30, ProductTypeId = 10, ProductAttributeId = 20, DisplayOrder = 7 });
            f.FailDefinitions = failure == "definitions";
            var request = new SaveProductTypeDesignRequestDTO
            {
                ProductType = new() { Id = 10, Name = "Changed" },
                Attributes = [new() { Id = 20, Name = "Changed attribute" }]
            };
            if (failure != "definitions") request.Attributes.Add(new()
            {
                Id = failure == "locked" ? 21 : failure == "missing" ? 999 : 0,
                Name = failure == "blank" ? " " : failure == "duplicate" ? "Changed attribute" : "Other"
            });
            var result = await f.Design.SaveDesignAsync(request);
            check(!result.Success && f.Saves == 0 && f.Writes == 0, $"Design {failure}: no writes or saves.");
            check(f.Types[0].Name == "Original" && f.Types[0].IsActive && f.Attributes[0].Name == "Original attribute"
                && f.Definitions[0].DisplayOrder == 7, $"Design {failure}: tracked rows remain unchanged.");
            check(result.ErrorCode == (failure == "definitions" ? ErrorCodes.Common.UnexpectedError
                : ErrorCodes.ProductTypeDesign.ErrorSavingProductAttribute), "Design preserves owner error contract.");
            if (failure != "definitions") check(result.ValidationErrors?.ContainsKey("Attributes") == true,
                "Secondary failure retains Attributes validation field.");
        }
        {
            var f = new AtomicFixture();
            f.Types.Add(new() { Id = 10, Name = "Type" });
            f.Attributes.Add(new() { Id = 20, Name = "A" });
            f.Definitions.AddRange([new() { Id = 30, ProductTypeId = 10, ProductAttributeId = 20 },
                new() { Id = 31, ProductTypeId = 10, ProductAttributeId = 20 }]);
            f.InUse.Add(30);
            var result = await f.Design.SaveDesignAsync(new()
            {
                ProductType = new() { Id = 10, Name = "Type updated" },
                Attributes = [new() { Id = 20, Name = "B" }, new() { Name = "A" }, new() { Id = 20, Name = "C" }],
                Definitions = [new() { Id = 999, ProductAttributeId = 20 }]
            });
            check(result.Success && f.Saves == 1 && f.Attributes[0].Name == "C" && f.Attributes[1].Name == "A",
                "Prepared renames free old names and repeated updates preserve sequential semantics.");
            check(f.Definitions.Count == 1 && f.Definitions[0].Id == 30, "In-use definitions retained; unused removed; unknown IDs skipped.");
        }
        {
            var f = new AtomicFixture();
            var result = await f.Design.SaveDesignAsync(new() { ProductType = new() { Name = " " } });
            check(result.ErrorCode == ErrorCodes.ProductTypeDesign.ProductTypeNameRequired && f.Saves == 0 && f.Writes == 0,
                "Design early validation.");
            f.Types.Add(new() { Id = 1, Name = "Locked", IsLocked = true });
            result = await f.Design.SaveDesignAsync(new() { ProductType = new() { Id = 1, Name = "Rename" } });
            check(result.Error == ErrorType.Conflict && result.ErrorCode == ErrorCodes.ProductTypeDesign.ProductTypeLocked && f.Writes == 0,
                "Locked type retains conflict contract.");
            result = await f.Design.SaveDesignAsync(new() { ProductType = new() { Name = "Locked" } });
            check(result.ErrorCode == ErrorCodes.ProductTypeDesign.ProductTypeNameAlreadyExists && f.Saves == 0,
                "Duplicate type validation.");
        }
        {
            var f = new AtomicFixture();
            var service = new UnifiedProductAttributeService(f.Uow);
            var create = await service.CreateAsync(new() { Name = " Attribute ", IsActive = true, Unit = "kg" });
            check(create.Success && create.Data?.Name == "Attribute" && f.Saves == 1, "Autonomous attribute create commits once.");
            var update = await service.UpdateAsync(new() { Id = create.Data!.Id, Name = "Renamed", IsLocked = true });
            check(update.Success && f.Saves == 2 && f.Attributes[0].IsLocked, "Autonomous attribute update commits once.");
            var invalid = await service.UpdateAsync(new() { Id = create.Data.Id, Name = "Rejected" });
            check(invalid.Message == ErrorMessages.UnifiedLegacy.ProductAttributeService_LockedCannotModifyMessage
                && invalid.ErrorCode == ErrorCodes.Common.InvalidRequest && f.Saves == 2 && f.Attributes[0].Name == "Renamed",
                "Locked attribute preserves error and state.");
            invalid = await service.CreateAsync(new() { Name = "Renamed" });
            check(invalid.Message == ErrorMessages.UnifiedLegacy.IndexingSourceService_NameAlreadyExistsMessage && f.Saves == 2,
                "Autonomous duplicate check preserved.");
        }
        foreach (var mode in new[] { "success", "blank", "query" })
        {
            var f = new AtomicFixture { FailConvocations = mode == "query" };
            f.Convocations.AddRange([new() { Id = 1, IsActive = true }, new() { Id = 2, IsActive = false }]);
            var result = await new UnifiedConvocationService(f.Uow).CreateAsync(new() { Name = mode == "blank" ? " " : " New ", Code = " C " });
            if (mode == "success")
            {
                check(result.Success && f.Saves == 1 && f.Convocations.Count == 3, "Convocation creates and activates with one commit.");
                check(!f.Convocations[0].IsActive && !f.Convocations[1].IsActive && f.Convocations[2].IsActive
                    && result.Data?.Name == "New" && result.Data.Code == "C", "Exclusive activation and normalization preserved.");
            }
            else
            {
                check(!result.Success && f.Saves == 0 && f.Writes == 0 && f.Convocations[0].IsActive,
                    "Convocation validation/query failure leaves tracked active row unchanged.");
                check(result.ErrorCode == (mode == "blank" ? ErrorCodes.Common.NameRequired : ErrorCodes.Common.UnexpectedError),
                    "Convocation error mapping preserved.");
            }
        }
        foreach (var mode in new[] { "success", "key", "duplicate", "fields" })
        {
            var f = new AtomicFixture { FailFields = mode == "fields" };
            f.Fields.Add(new() { Id = 7, Key = "field", DefaultHeader = "Default" });
            if (mode == "duplicate") f.Templates.Add(new() { Id = 1, Key = "key" });
            var request = new ExportTemplateCreateRequestDTO
            {
                Key = mode == "key" ? " " : " key ", Name = " Template ",
                Columns = [new() { FieldId = 7, OrderIndex = 2, TargetHeader = " Custom ", Format = " N2 ", Separator = " ; " },
                    new() { FieldId = 999, OrderIndex = 0 }, new() { FieldId = 7, OrderIndex = 1 }]
            };
            try
            {
                var result = await new UnifiedExportTemplateService(f.Uow, NullLogger<UnifiedExportTemplateService>.Instance)
                    .CreateTemplateAsync(request, default);
                check(mode != "fields", "Expected field lookup failure must propagate.");
                if (mode == "success")
                {
                    check(result.Success && f.Saves == 1 && f.Templates.Count == 1, "Template aggregate commits once.");
                    var columns = f.Templates.Single().Columns.OrderBy(x => x.OrderIndex).ToList();
                    check(columns.Count == 2 && columns[0].TargetHeader == "Default" && columns[1].TargetHeader == "Custom"
                        && columns[1].Format == "N2" && columns[1].Separator == ";", "Column defaults, trimming, order and unknown-field skip preserved.");
                    check(columns.All(x => x.TemplateId > 0 && ReferenceEquals(x.Template, f.Templates[0])), "Template generated FK graph.");
                }
                else check(!result.Success && f.Saves == 0 && f.Writes == 0 && result.ErrorCode == (mode == "key"
                    ? ErrorCodes.ExportTemplate.KeyRequired : ErrorCodes.ExportTemplate.KeyAlreadyExists), "Template early validation preserves errors.");
            }
            catch (TestReadException)
            {
                check(mode == "fields" && f.Saves == 0 && f.Writes == 0 && f.Templates.Count == 0,
                    "Failed column preparation never stages header.");
            }
        }
        VerifyEfGraph(check);
        await VerifyTrackedServiceAsync(check);
        foreach (var operation in new[] { "design", "convocation", "template" })
        {
            var f = new AtomicFixture { OnSave = () => throw new DbUpdateException("Injected save failure") };
            if (operation == "design")
            {
                var result = await f.Design.SaveDesignAsync(new() { ProductType = new() { Name = "Type" } });
                check(!result.Success && result.ErrorCode == ErrorCodes.Common.UnexpectedError, "Design save error retains mapping.");
            }
            else if (operation == "convocation")
            {
                var result = await new UnifiedConvocationService(f.Uow).CreateAsync(new() { Name = "Convocation" });
                check(!result.Success && result.Error == ErrorType.Conflict, "Convocation save error retains conflict mapping.");
            }
            else
            {
                var threw = false;
                try
                {
                    await new UnifiedExportTemplateService(f.Uow, NullLogger<UnifiedExportTemplateService>.Instance)
                        .CreateTemplateAsync(new() { Key = "key", Name = "Template" }, default);
                }
                catch (DbUpdateException) { threw = true; }
                check(threw, "Template save error continues to propagate.");
            }
            check(f.Saves == 1 && f.Writes > 0, "Failed final save has one attempt and pending state: discard scope.");
        }
    }

    private static void VerifyEfGraph(Action<bool, string> check)
    {
        using var db = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Trusted_Connection=True;TrustServerCertificate=True").Options);
        var type = new ProductType { Name = "Type" };
        var attribute = new ProductAttribute { Name = "Attribute" };
        var definition = new ProductAttributeDefinition { ProductType = type, ProductAttribute = attribute };
        db.Add(type); db.Add(attribute); db.Add(definition);
        var template = new ExportTemplate { Key = "key", Name = "Template" };
        template.Columns.Add(new() { Template = template, ExportFieldId = 7, TargetHeader = "Header" });
        db.Add(template);
        db.ChangeTracker.DetectChanges();
        check(db.Entry(definition).Property(x => x.ProductTypeId).CurrentValue == db.Entry(type).Property(x => x.Id).CurrentValue
            && db.Entry(definition).Property(x => x.ProductAttributeId).CurrentValue == db.Entry(attribute).Property(x => x.Id).CurrentValue,
            "Real EF tracks temporary design keys via navigation without intermediate saves.");
        check(db.Entry(template.Columns.Single()).Property(x => x.TemplateId).CurrentValue == db.Entry(template).Property(x => x.Id).CurrentValue
            && db.ChangeTracker.Entries<ExportField>().Count() == 0, "EF tracks export aggregate without inserting referenced field.");
        var preparedName = "Prepared";
        var name = "Candidate";
        var sql = db.Set<ProductAttribute>().Select(x => x.Id).Take(0).DefaultIfEmpty().Select(_ => preparedName)
            .Where(candidate => candidate == name).ToQueryString();
        check(sql.Contains("WHERE") && sql.Contains("Prepared") && sql.Contains("Candidate") && sql.Contains("LEFT JOIN"),
            "SQL Server translates comparison of pending names, including an empty catalog, without connecting.");
    }

    private static async Task VerifyTrackedServiceAsync(Action<bool, string> check)
    {
        using var db = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Trusted_Connection=True;TrustServerCertificate=True").Options);
        var type = new ProductType { Id = 10, Name = "Original" };
        var attribute = new ProductAttribute { Id = 20, Name = "Original attribute" };
        var definition = new ProductAttributeDefinition { Id = 30, ProductTypeId = 10, ProductType = type,
            ProductAttributeId = 20, ProductAttribute = attribute, DisplayOrder = 1 };
        db.Attach(definition);
        var f = new AtomicFixture { OnAdd = entity => db.Add(entity), OnUpdate = entity => db.Update(entity) };
        f.Types.Add(type); f.Attributes.Add(attribute); f.Definitions.Add(definition);
        var invalid = await f.Design.SaveDesignAsync(new()
        {
            ProductType = new() { Id = 10, Name = "Changed" },
            Attributes = [new() { Id = 20, Name = "Changed attribute" }, new() { Name = " " }]
        });
        check(!invalid.Success && !db.ChangeTracker.HasChanges() && f.Saves == 0,
            "Real EF tracker has no pending mutations after secondary validation failure.");
        f.OnSave = () =>
        {
            db.ChangeTracker.DetectChanges();
            var added = db.ChangeTracker.Entries<ProductAttribute>().Single(x => x.State == EntityState.Added);
            check(db.Entry(definition).Property(x => x.ProductAttributeId).CurrentValue == added.Property(x => x.Id).CurrentValue
                && ReferenceEquals(definition.ProductAttribute, added.Entity),
                "Actual design service binds existing definition to new attribute temporary FK before its only save.");
        };
        var result = await f.Design.SaveDesignAsync(new()
        {
            ProductType = new() { Id = 10, Name = "Changed" },
            Attributes = [new() { Name = "New attribute" }],
            Definitions = [new() { Id = 30, ProductAttributeId = 0, DisplayOrder = 4 }]
        });
        check(result.Success && f.Saves == 1 && definition.DisplayOrder == 4, "Tracked update uses one owner commit.");
    }
}

internal sealed class TestReadException : Exception { }

internal sealed class AtomicFixture
{
    internal readonly List<ProductType> Types = [];
    internal readonly List<ProductAttribute> Attributes = [];
    internal readonly List<ProductAttributeDefinition> Definitions = [];
    internal readonly List<Convocation> Convocations = [];
    internal readonly List<ExportTemplate> Templates = [];
    internal readonly List<ExportField> Fields = [];
    internal readonly HashSet<int> InUse = [];
    internal int Saves, Writes;
    internal bool FailDefinitions, FailConvocations, FailFields;
    internal Action<object>? OnAdd, OnUpdate;
    internal Action? OnSave;
    internal IUnifiedUnitOfWork Uow { get; }
    internal UnifiedProductTypeDesignService Design => new(Uow);

    internal AtomicFixture()
    {
        var types = Catalog(Types);
        var attributes = Catalog(Attributes);
        var definitions = Stub.For<IUnifiedProductAttributeDefinitionRepository>((m, a) =>
        {
            switch (m.Name)
            {
                case "GetByTypeAsync":
                    if (FailDefinitions) throw new TestReadException();
                    return Task.FromResult(Definitions.Where(x => x.ProductTypeId == (int)a[0]!).ToList());
                case "QueryByType": return new AsyncRows<ProductAttributeDefinition>(Definitions.Where(x => x.ProductTypeId == (int)a[0]!));
                case "AnyValuesUsingDefinitionAsync": return Task.FromResult(InUse.Contains((int)a[0]!));
                case "AddAsync": Writes++; OnAdd?.Invoke(a[0]!); Definitions.Add((ProductAttributeDefinition)a[0]!); return Task.CompletedTask;
                case "Update": Writes++; OnUpdate?.Invoke(a[0]!); return null;
                case "Remove": Writes++; Definitions.Remove((ProductAttributeDefinition)a[0]!); return null;
                default: throw new InvalidOperationException(m.Name);
            }
        });
        var convocations = Stub.For<IUnifiedConvocationRepository>((m, a) =>
        {
            if (m.Name == "Query")
            {
                if (FailConvocations) throw new TestReadException();
                if ((bool)a[0]!) throw new InvalidOperationException("Convocation activation needs tracked rows.");
                return new AsyncRows<Convocation>(Convocations);
            }
            if (m.Name == "AddAsync") { Writes++; Convocations.Add((Convocation)a[0]!); return Task.CompletedTask; }
            throw new InvalidOperationException(m.Name);
        });
        var templates = Stub.For<IUnifiedExportTemplateRepository>((m, a) => m.Name switch
        {
            "GetByKeyAsync" => Task.FromResult(Templates.FirstOrDefault(x => x.Key == (string)a[0]!)),
            "GetDetailByIdAsync" => Task.FromResult(Templates.FirstOrDefault(x => x.Id == (int)a[0]!)),
            "AddAsync" => AddTemplate((ExportTemplate)a[0]!),
            _ => throw new InvalidOperationException(m.Name)
        });
        var fields = Stub.For<IUnifiedExportFieldRepository>((m, a) =>
        {
            if (FailFields) throw new TestReadException();
            if (m.Name == "ListAsync") return Task.FromResult<IReadOnlyList<ExportField>>(Fields.ToList());
            throw new InvalidOperationException(m.Name);
        });
        Uow = Stub.For<IUnifiedUnitOfWork>((m, a) => m.Name switch
        {
            "get_ProductTypes" => types, "get_ProductAttributes" => attributes,
            "get_ProductAttributeDefinitions" => definitions, "get_Convocations" => convocations,
            "get_ExportTemplates" => templates, "get_ExportFields" => fields,
            "SaveChangesAsync" => Save(), _ => throw new InvalidOperationException(m.Name)
        });
    }

    private IUnifiedCatalogRepository<T> Catalog<T>(List<T> rows) where T : CatalogEntityBase =>
        Stub.For<IUnifiedCatalogRepository<T>>((m, a) =>
        {
            switch (m.Name)
            {
                case "NameExistsAsync": return Task.FromResult(rows.Any(x => x.Name == (string)a[0]! && x.Id != (int?)a[1]));
                case "GetByIdAsync": return Task.FromResult(rows.FirstOrDefault(x => x.Id == (int)((object[])a[0]!)[0]));
                case "ListAsync": return Task.FromResult<IReadOnlyList<T>>(rows.Where(x => !(bool)a[0]! || x.IsActive).ToList());
                case "Query": return new AsyncRows<T>(rows);
                case "AddAsync": Writes++; OnAdd?.Invoke(a[0]!); rows.Add((T)a[0]!); return Task.CompletedTask;
                case "Update": Writes++; OnUpdate?.Invoke(a[0]!); return null;
                default: throw new InvalidOperationException(m.Name);
            }
        });
    private Task AddTemplate(ExportTemplate template) { Writes++; Templates.Add(template); return Task.CompletedTask; }
    private Task<int> Save()
    {
        Saves++;
        OnSave?.Invoke();
        var id = 100;
        foreach (var row in Types.Cast<CatalogEntityBase>().Concat(Attributes)) if (row.Id == 0) row.Id = ++id;
        foreach (var row in Definitions)
        {
            if (row.Id == 0) row.Id = ++id;
            if (row.ProductType is not null) row.ProductTypeId = row.ProductType.Id;
            if (row.ProductAttribute is not null) row.ProductAttributeId = row.ProductAttribute.Id;
        }
        foreach (var row in Convocations) if (row.Id == 0) row.Id = ++id;
        foreach (var row in Templates)
        {
            if (row.Id == 0) row.Id = ++id;
            foreach (var column in row.Columns) { column.Id = ++id; column.TemplateId = row.Id; }
        }
        return Task.FromResult(Writes);
    }
}

internal sealed class AsyncRows<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    internal AsyncRows(IEnumerable<T> rows) : base(rows) { }
    internal AsyncRows(Expression expression) : base(expression) { }
    IQueryProvider IQueryable.Provider => new AsyncRowProvider(this);
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new AsyncRowEnumerator<T>(this.AsEnumerable().GetEnumerator(), cancellationToken);
}
internal sealed class AsyncRowEnumerator<T>(IEnumerator<T> inner, CancellationToken token) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;
    public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() { token.ThrowIfCancellationRequested(); return ValueTask.FromResult(inner.MoveNext()); }
}
internal sealed class AsyncRowProvider(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new AsyncRows<TElement>(expression);
    public object? Execute(Expression expression) => inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = inner.Execute(expression);
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(typeof(TResult).GenericTypeArguments[0])
            .Invoke(null, [value])!;
    }
}
