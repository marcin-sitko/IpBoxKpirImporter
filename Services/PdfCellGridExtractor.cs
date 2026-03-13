using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IpBoxKpirImporter.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace IpBoxKpirImporter.Services;

public sealed class PdfCellGridExtractor
{
    private readonly string debugFolder;
    private static readonly Regex DateRegex = new(@"\b\d{2}-\d{2}-\d{4}\b", RegexOptions.Compiled);

    public PdfCellGridExtractor(string debugFolder)
    {
        this.debugFolder = debugFolder;
    }

    public List<KpirTableRow> Extract(string pdfPath)
    {
        var temp = Path.Combine(Path.GetTempPath(), "IpBoxKpirImporter", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);

        try
        {
            var prefix = Path.Combine(temp, "page");
            ProcessRunner.Run("pdftoppm", $"-png -r 300 \"{pdfPath}\" \"{prefix}\"");

            var results = new List<KpirTableRow>();
            var baseName = Path.GetFileNameWithoutExtension(pdfPath);
            List<int>? inheritedVertical = null;

            using var document = PdfDocument.Open(pdfPath);
            foreach (var page in document.GetPages())
            {
                var imagePath = Path.Combine(temp, $"page-{page.Number}.png");
                if (!File.Exists(imagePath))
                    continue;

                var lines = ImageGridDetector.Detect(imagePath);
                if ((lines.Vertical == null || lines.Vertical.Count < 17) && inheritedVertical != null)
                    lines.Vertical = new List<int>(inheritedVertical);
                else if (lines.Vertical != null && lines.Vertical.Count >= 17)
                    inheritedVertical = new List<int>(lines.Vertical);

                var words = ExtractWords(page, lines.Width, lines.Height);
                var cells = BuildCells(lines);
                var rows = SerializeRows(words, cells, page.Number);

                SaveDebug(baseName, page.Number, lines, rows);
                results.AddRange(rows);
            }

            return results
                .Where(IsDataRow)
                .OrderBy(r => ParseDateSafe(r.Date))
                .ThenBy(r => ParseIntSafe(r.Lp))
                .ToList();
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { }
        }
    }

    private void SaveDebug(string baseName, int pageNumber, ImageGridDetector.GridLines lines, List<KpirTableRow> rows)
    {
        File.WriteAllLines(Path.Combine(debugFolder, $"{baseName}_cellgrid_lines_page_{pageNumber}.txt"), new[]
        {
            "Horizontal: " + string.Join(", ", lines.Horizontal),
            "Vertical: " + string.Join(", ", lines.Vertical)
        });

        File.WriteAllLines(Path.Combine(debugFolder, $"{baseName}_cellgrid_rows_page_{pageNumber}.txt"),
            rows.Select(r =>
                $"ROW p{r.PageNumber}#{r.RowIndexOnPage} LP=[{r.Lp}] DATE=[{r.Date}] DOC=[{r.DocumentNumber}] CP=[{r.Counterparty}] DESC=[{r.Description}] REV9=[{r.RevenueGoodsAndServices}] REV10=[{r.RevenueOther}] REV11=[{r.RevenueTotal}] C12=[{r.PurchaseGoods}] C13=[{r.IncidentalPurchaseCosts}] C14=[{r.SalaryInCash}] C15=[{r.OtherExpense}] C16=[{r.ExpenseTotal}] FULL=[{r.FullRowText}]"));
    }

    private static List<PdfWord> ExtractWords(Page page, int imageWidth, int imageHeight)
    {
        var sx = imageWidth / page.Width;
        var sy = imageHeight / page.Height;

        return page.GetWords()
            .Select(w => new PdfWord
            {
                Text = NormalizeText(w.Text),
                PxX0 = w.BoundingBox.Left * sx,
                PxX1 = w.BoundingBox.Right * sx,
                PxY0 = imageHeight - (w.BoundingBox.Top * sy),
                PxY1 = imageHeight - (w.BoundingBox.Bottom * sy)
            })
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .ToList();
    }

    private static List<TableCell> BuildCells(ImageGridDetector.GridLines lines)
    {
        var cells = new List<TableCell>();
        var hs = lines.Horizontal.OrderBy(x => x).ToList();
        var vs = lines.Vertical.OrderBy(x => x).ToList();

        if (vs.Count < 17 || hs.Count < 2)
            return cells;

        for (var r = 0; r < hs.Count - 1; r++)
        {
            var top = hs[r];
            var bottom = hs[r + 1];
            if (bottom - top < 18)
                continue;

            for (var c = 0; c < vs.Count - 1; c++)
            {
                cells.Add(new TableCell
                {
                    RowIndex = r + 1,
                    ColumnIndex = c + 1,
                    Left = vs[c],
                    Right = vs[c + 1],
                    Top = top,
                    Bottom = bottom
                });
            }
        }

        return cells;
    }

    private static List<KpirTableRow> SerializeRows(List<PdfWord> words, List<TableCell> cells, int pageNumber)
    {
        var rows = new List<KpirTableRow>();
        if (cells.Count == 0)
            return rows;

        var groupedRows = cells.GroupBy(c => c.RowIndex).OrderBy(g => g.Key).ToList();

        foreach (var rowGroup in groupedRows)
        {
            var cellsByCol = rowGroup.OrderBy(c => c.ColumnIndex).ToDictionary(c => c.ColumnIndex, c => c);
            var buckets = new Dictionary<int, List<PdfWord>>();
            foreach (var col in cellsByCol.Keys)
                buckets[col] = new List<PdfWord>();

            foreach (var w in words.Where(w => rowGroup.Any(c => w.CenterPxY >= c.Top && w.CenterPxY < c.Bottom)))
            {
                var overlapping = rowGroup
                    .Select(c => new { Cell = c, Overlap = c.OverlapWidth(w.PxX0, w.PxX1) })
                    .Where(x => x.Overlap > 0)
                    .OrderByDescending(x => x.Overlap)
                    .ThenBy(x => x.Cell.ColumnIndex)
                    .ToList();

                if (overlapping.Count > 0)
                {
                    buckets[overlapping[0].Cell.ColumnIndex].Add(w);
                }
                else
                {
                    var nearest = rowGroup.OrderBy(c => Math.Abs(((c.Left + c.Right) / 2.0) - w.CenterPxX)).First();
                    buckets[nearest.ColumnIndex].Add(w);
                }
            }

            // numeric carry for amount-like cells split across adjacent columns
            MergeAmountPairs(buckets, 9, 10);
            MergeAmountPairs(buckets, 10, 11);
            MergeAmountPairs(buckets, 12, 13);
            MergeAmountPairs(buckets, 13, 14);
            MergeAmountPairs(buckets, 14, 15);
            MergeAmountPairs(buckets, 15, 16);

            var byCol = buckets.ToDictionary(
                kv => kv.Key,
                kv => JoinCellWords(kv.Value));

            var row = new KpirTableRow
            {
                PageNumber = pageNumber,
                RowIndexOnPage = rowGroup.Key,
                Lp = JoinColumns(byCol, 1),
                Date = ExtractDateCell(JoinColumns(byCol, 2, 3)),
                DocumentNumber = JoinColumns(byCol, 4),
                Counterparty = JoinColumns(byCol, 5),
                Description = JoinColumns(byCol, 6, 7, 8),
                RevenueGoodsAndServices = JoinColumns(byCol, 9),
                RevenueOther = JoinColumns(byCol, 10),
                RevenueTotal = JoinColumns(byCol, 11),
                PurchaseGoods = JoinColumns(byCol, 12),
                IncidentalPurchaseCosts = JoinColumns(byCol, 13),
                SalaryInCash = JoinColumns(byCol, 14),
                OtherExpense = JoinColumns(byCol, 15),
                ExpenseTotal = JoinColumns(byCol, 16),
                Notes = JoinColumns(byCol, 17),
                FullRowText = NormalizeCell(string.Join(" ", byCol.OrderBy(k => k.Key).Select(k => k.Value))),
                RawByColumn = byCol
            };

            rows.Add(row);
        }

        return rows;
    }

    private static void MergeAmountPairs(Dictionary<int, List<PdfWord>> buckets, int leftCol, int rightCol)
    {
        if (!buckets.ContainsKey(leftCol) || !buckets.ContainsKey(rightCol))
            return;

        var leftText = NormalizeCell(string.Join(" ", buckets[leftCol].Select(w => w.Text)));
        var rightText = NormalizeCell(string.Join(" ", buckets[rightCol].Select(w => w.Text)));

        // If left side looks like integer part and right side like grosze / duplicated money, keep both as-is.
        // If right side is empty and left has numeric words, nothing to do.
        if (string.IsNullOrWhiteSpace(leftText) || string.IsNullOrWhiteSpace(rightText))
            return;

        var leftNumeric = Regex.IsMatch(leftText, @"^\d{1,3}(?: \d{3})*$");
        var rightNumeric = Regex.IsMatch(rightText, @"^\d{2}$");
        if (leftNumeric && rightNumeric)
            return;

        // If a value is duplicated over pair and next column is empty later, better keep current bucket assignment.
    }


    private static string ExtractDateCell(string text)
    {
        text = NormalizeCell(text);
        var m = DateRegex.Match(text ?? "");
        return m.Success ? m.Value : text;
    }

    private static string JoinColumns(Dictionary<int, string> byCol, params int[] cols)
    {
        return NormalizeCell(string.Join(" ", cols.Where(byCol.ContainsKey).Select(c => byCol[c])));
    }

    private static string JoinCellWords(List<PdfWord> words)
    {
        if (words.Count == 0)
            return string.Empty;

        const double yTolerance = 8.0;
        var lines = new List<List<PdfWord>>();

        foreach (var w in words.OrderBy(x => x.CenterPxY).ThenBy(x => x.PxX0))
        {
            var line = lines.FirstOrDefault(l => Math.Abs(l.Average(z => z.CenterPxY) - w.CenterPxY) <= yTolerance);
            if (line == null)
            {
                line = new List<PdfWord>();
                lines.Add(line);
            }
            line.Add(w);
        }

        var orderedLines = lines
            .OrderBy(l => l.Average(z => z.CenterPxY))
            .Select(l => string.Join(" ", l.OrderBy(z => z.PxX0).Select(z => z.Text)))
            .ToList();

        return NormalizeCell(string.Join(" ", orderedLines));
    }

    private static bool IsDataRow(KpirTableRow row)
    {
        if (!int.TryParse(ExtractFirstInteger(row.Lp), out var lp) || lp <= 0 || lp > 500)
            return false;

        if (!DateRegex.IsMatch(row.Date) && !DateRegex.IsMatch(row.FullRowText))
            return false;

        var full = row.FullRowText.ToLowerInvariant();
        if (full.Contains("data wydruku")
            || full.Contains("podatkowa ksi")
            || (full.Contains("lp") && full.Contains("zdarzenia"))
            || full.Contains("nr identyfik")
            || full.Contains("oznaczenie dowodu")
            || full.Contains("identyfikator podatkowy")
            || full.Contains("suma folio")
            || full.Contains("przeniesienie z folio")
            || full.StartsWith("razem")
            || full.Contains("koszty działalności badawczo"))
            return false;

        return true;
    }

    private static string NormalizeCell(string text) => Regex.Replace(text ?? "", @"\s+", " ").Trim();
    private static string NormalizeText(string text) => text.Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static int ParseIntSafe(string text) => int.TryParse(ExtractFirstInteger(text), out var n) ? n : int.MaxValue;

    private static DateTime ParseDateSafe(string text)
    {
        var m = DateRegex.Match(text ?? "");
        return DateTime.TryParseExact(m.Value, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.MaxValue;
    }

    private static string ExtractFirstInteger(string text)
    {
        var m = Regex.Match(text ?? "", @"\b\d{1,3}\b");
        return m.Success ? m.Value : "";
    }
}
