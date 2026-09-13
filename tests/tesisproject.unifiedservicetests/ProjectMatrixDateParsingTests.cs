using System.Globalization;
using ClosedXML.Excel;
using tesisproject.backend.Services.Unified.Implementations;

internal static class ProjectMatrixDateParsingTests
{
    internal static void Run(Action<bool, string> check)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            check(
                UnifiedProjectMatrixService.ParseDate("04/12/2013") == new DateTime(2013, 12, 4),
                "Project matrix must interpret 04/12/2013 as dd/MM/yyyy.");
            check(
                UnifiedProjectMatrixService.ParseDate("12/04/2013") == new DateTime(2013, 4, 12),
                "Project matrix must interpret 12/04/2013 as dd/MM/yyyy.");
            check(
                UnifiedProjectMatrixService.ParseDate("18/09/2017") == new DateTime(2017, 9, 18),
                "Project matrix must accept days greater than 12 without MM/dd fallback.");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Dates");
            var dateCell = worksheet.Cell("A1");
            dateCell.Value = new DateTime(2013, 12, 4, 16, 30, 0);
            dateCell.Style.DateFormat.Format = "mm-dd-yy";

            var typedDate = UnifiedProjectMatrixService.GetDeterministicCellValue(dateCell);
            check(typedDate == "2013-12-04", "A real Excel DateTime must be read from its typed value, not its display format.");
            check(
                UnifiedProjectMatrixService.ParseDate(typedDate) == new DateTime(2013, 12, 4),
                "A real Excel DateTime must preserve its calendar date through the matrix pipeline.");

            var serialCell = worksheet.Cell("A2");
            serialCell.Value = new DateTime(2013, 12, 4).ToOADate();
            var serial = UnifiedProjectMatrixService.GetDeterministicCellValue(serialCell);
            check(
                UnifiedProjectMatrixService.ParseDate(serial) == new DateTime(2013, 12, 4),
                "A valid Excel serial must resolve deterministically.");

            check(
                UnifiedProjectMatrixService.ParseDate(
                    UnifiedProjectMatrixService.GetDeterministicCellValue(worksheet.Cell("A3"))) is null,
                "An empty Excel cell must remain null.");
            check(UnifiedProjectMatrixService.ParseDate(null) is null, "A null date must remain null.");
            check(UnifiedProjectMatrixService.ParseDate("#VALUE!") is null, "Invalid date text must remain null.");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
