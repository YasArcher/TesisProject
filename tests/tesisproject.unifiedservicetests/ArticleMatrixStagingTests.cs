using System.Text.Json;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.shared.DTOs.MassRegistration;

internal static class ArticleMatrixStagingTests
{
    public static void Run(Action<bool, string> check)
    {
        var matrix = new RegistrationMatrix { RegistrationMatrixId = 4, Name = "Test" };
        matrix.Columns.Add(new RegistrationMatrixColumn
        {
            FieldId = 1, Field = new FieldCatalogEntry { FieldId = 1, FieldKey = "Title", EntityName = "Article", FieldLabel = "Titulo", IsRequired = true }
        });
        matrix.Columns.Add(new RegistrationMatrixColumn
        {
            FieldId = 2, Field = new FieldCatalogEntry { FieldId = 2, FieldKey = "Doi", EntityName = "Article", FieldLabel = "DOI" }
        });
        matrix.Rows.Add(Row(11, 1, "First", "https://doi.org/10.1/ABC"));
        matrix.Rows.Add(Row(12, 2, "Second", "10.1/abc"));
        matrix.Rows.Add(Row(13, 3, null, "10.1/new"));
        matrix.Rows.Add(Row(14, 4, "Existing", "10.1/known"));
        matrix.Rows.Add(Row(15, 5, "Long DOI", "10.12345678901234567890/long"));

        var planned = ArticleMatrixStagingPlanner.BuildRows(matrix, new HashSet<string> { "10.1/known" }, []);
        check(planned.Select(x => x.Status).SequenceEqual(["Ready", "Duplicate", "Invalid", "Duplicate", "Invalid"]),
            "Matrix staging must separate ready rows, repeated DOIs, missing fields and existing DOIs.");
        check(planned[0].DoiNormalized == "10.1/abc" && planned[1].DoiNormalized == "10.1/abc",
            "DOI normalization must be stable across URL and bare forms.");
        check(planned[2].ValidationMessage?.Contains("Titulo") == true,
            "Required-field errors must name the field a user can correct.");
        check(planned[4].ValidationMessage?.Contains("20 caracteres") == true,
            "Legacy matrix values cannot bypass the DOI length limit at submission.");
        using var snapshot = JsonDocument.Parse(planned[0].SnapshotJson);
        check(snapshot.RootElement.GetProperty("Fields")[0].GetProperty("FieldKey").GetString() == "Title",
            "Staging must preserve field metadata independently of later form changes.");
    }

    private static RegistrationMatrixRow Row(int id, int number, string? title, string doi)
    {
        var row = new RegistrationMatrixRow { RegistrationMatrixRowId = id, RowNumber = number };
        if (title is not null) row.Cells.Add(new RegistrationMatrixCell { FieldId = 1, RawValue = title });
        row.Cells.Add(new RegistrationMatrixCell { FieldId = 2, RawValue = doi });
        return row;
    }
}
