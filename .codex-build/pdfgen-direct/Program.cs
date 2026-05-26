using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net;
using System.Text.RegularExpressions;

QuestPDF.Settings.License = LicenseType.Community;
var root = Directory.GetCurrentDirectory();
while (!File.Exists(Path.Combine(root, "TesisProject.sln")) && Directory.GetParent(root) is not null)
    root = Directory.GetParent(root)!.FullName;
var mdPath = Path.Combine(root, "docs", "matriz-defendible-tiempos-reporteria-tesis.md");
var pdfPath = Path.Combine(root, "docs", "matriz-defendible-tiempos-reporteria-tesis.pdf");
var lines = File.ReadAllLines(mdPath);
var blocks = Parse(lines);
Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(32);
        page.DefaultTextStyle(x => x.FontSize(9.2f).FontFamily("Segoe UI"));
        page.Header().Column(col =>
        {
            col.Item().Text("Matriz defendible de tiempos de reportería").FontSize(16).Bold().FontColor("003638");
            col.Item().PaddingTop(3).LineHorizontal(1).LineColor("408A71");
        });
        page.Content().PaddingTop(12).Column(col =>
        {
            col.Spacing(7);
            foreach (var block in blocks) RenderBlock(col, block);
        });
        page.Footer().AlignRight().Text(text =>
        {
            text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Darken1);
            text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
            text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Darken1);
            text.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    });
}).GeneratePdf(pdfPath);
Console.WriteLine(pdfPath);

static List<Block> Parse(string[] lines)
{
    var result = new List<Block>(); var inCode = false; var code = new List<string>();
    for (var i = 0; i < lines.Length; i++)
    {
        var line = lines[i];
        if (line.TrimStart().StartsWith("```")) { if (!inCode) { inCode = true; code.Clear(); } else { result.Add(new CodeBlock(string.Join("\n", code))); inCode = false; } continue; }
        if (inCode) { code.Add(line); continue; }
        if (string.IsNullOrWhiteSpace(line)) continue;
        if (line.StartsWith("# ")) { result.Add(new HeadingBlock(1, line[2..].Trim())); continue; }
        if (line.StartsWith("## ")) { result.Add(new HeadingBlock(2, line[3..].Trim())); continue; }
        if (line.StartsWith("### ")) { result.Add(new HeadingBlock(3, line[4..].Trim())); continue; }
        if (line.StartsWith("- ")) { result.Add(new ParagraphBlock("• " + line[2..].Trim())); continue; }
        if (line.TrimStart().StartsWith("|"))
        {
            var rows = new List<string[]>();
            while (i < lines.Length && lines[i].TrimStart().StartsWith("|"))
            {
                var cells = lines[i].Trim().Trim('|').Split('|').Select(x => x.Trim()).ToArray();
                var separator = cells.All(x => Regex.IsMatch(x, "^:?-{3,}:?$"));
                if (!separator) rows.Add(cells); i++;
            }
            i--; if (rows.Count > 0) result.Add(new TableBlock(rows)); continue;
        }
        result.Add(new ParagraphBlock(line.Trim()));
    }
    return result;
}
static void RenderBlock(ColumnDescriptor col, Block block)
{
    switch (block)
    {
        case HeadingBlock h: col.Item().PaddingTop(h.Level == 1 ? 0 : 8).Text(Clean(h.Text)).FontSize(h.Level == 1 ? 18 : h.Level == 2 ? 13 : 11).Bold().FontColor(h.Level == 1 ? "003638" : h.Level == 2 ? "055052" : "285A48"); break;
        case ParagraphBlock p: col.Item().Text(Clean(p.Text)).FontSize(9.2f).LineHeight(1.18f); break;
        case CodeBlock c: col.Item().Background("F3F6F5").BorderLeft(3).BorderColor("408A71").Padding(6).Text(c.Text).FontFamily("Consolas").FontSize(8.4f).FontColor("660B05"); break;
        case TableBlock t: RenderTable(col, t.Rows); break;
    }
}
static void RenderTable(ColumnDescriptor col, List<string[]> rows)
{
    var columns = rows.Max(x => x.Length);
    col.Item().Table(table =>
    {
        table.ColumnsDefinition(def => { for (var i = 0; i < columns; i++) def.RelativeColumn(); });
        for (var r = 0; r < rows.Count; r++)
        for (var c = 0; c < columns; c++)
        {
            var value = c < rows[r].Length ? Clean(rows[r][c]) : string.Empty;
            var cell = table.Cell().Border(0.5f).BorderColor("CBD5D1").Padding(4);
            if (r == 0) cell.Background("003638").Text(value).FontColor(Colors.White).Bold().FontSize(8.0f);
            else cell.Background(r % 2 == 0 ? "F1F7F4" : "FBFDFC").Text(value).FontSize(7.5f).LineHeight(1.08f);
        }
    });
}
static string Clean(string text) => WebUtility.HtmlDecode(text).Replace("`", "");
abstract record Block; record HeadingBlock(int Level, string Text) : Block; record ParagraphBlock(string Text) : Block; record CodeBlock(string Text) : Block; record TableBlock(List<string[]> Rows) : Block;
