using System.Text.Json;
using IpBoxKpirImporter.Models;
using IpBoxKpirImporter.Services;
using Xunit;

namespace IpBoxKpirImporter.Tests;

public class ReferencePdfTests
{
    [Fact]
    public void January2026_reference_pdf_should_parse_expected_rows_entries_and_lp_sequence()
    {
        var root = GetProjectRoot();
        var testDataDir = Path.Combine(root, "test-data");
        var pdfPath = Path.Combine(testDataDir, "KPiR 01_2026.pdf");
        var expectedPath = Path.Combine(testDataDir, "expected.json");
        var debugOutput = Path.Combine(root, "test-output");

        Assert.True(File.Exists(pdfPath), $"Missing reference PDF: {pdfPath}");
        Assert.True(File.Exists(expectedPath), $"Missing expected.json: {expectedPath}");

        Directory.CreateDirectory(debugOutput);

        var expected = JsonSerializer.Deserialize<Expected>(
            File.ReadAllText(expectedPath))
            ?? throw new InvalidOperationException("Could not read expected.json");

        var extractor = new PdfCellGridExtractor(debugOutput, false);
        var rows = extractor.Extract(pdfPath);

        var mapper = new KpirRowMapper();
        var entries = rows
            .Select(mapper.Map)
            .Where(x => x != null)
            .Cast<KpirEntry>()
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Lp)
            .ToList();

        Assert.Equal(expected.expected_table_rows, rows.Count);
        Assert.Equal(expected.expected_entries, entries.Count);
        Assert.Equal(expected.expected_lp, entries.Select(e => e.Lp).ToList());
    }

    private static string GetProjectRoot()
    {
        // test bin -> project root (IpBoxKpirImporter)
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    private sealed class Expected
    {
        public string file { get; set; } = "";
        public int expected_table_rows { get; set; }
        public int expected_entries { get; set; }
        public List<int> expected_lp { get; set; } = [];
        public string notes { get; set; } = "";
    }
}
