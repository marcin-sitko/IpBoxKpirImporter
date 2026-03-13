using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using IpBoxKpirImporter.Models;

namespace IpBoxKpirImporter.Services;

public static class ExcelUpdater
{
    private static readonly Dictionary<int, string> MonthNamesPl = new()
    {
        [1] = "Styczeń",
        [2] = "Luty",
        [3] = "Marzec",
        [4] = "Kwiecień",
        [5] = "Maj",
        [6] = "Czerwiec",
        [7] = "Lipiec",
        [8] = "Sierpień",
        [9] = "Wrzesień",
        [10] = "Październik",
        [11] = "Listopad",
        [12] = "Grudzień",
    };

    public static void WriteImportWorkbook(string path, List<KpirEntry> entries)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Entries");
        var headers = new[] { "Lp", "Date", "Type", "Document", "Description", "Amount", "Excluded" };

        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        var row = 2;
        foreach (var e in entries.OrderBy(x => x.Date).ThenBy(x => x.Lp))
        {
            ws.Cell(row, 1).Value = e.Lp;
            ws.Cell(row, 2).Value = e.Date;
            ws.Cell(row, 3).Value = e.EntryType.ToString();
            ws.Cell(row, 4).Value = e.DocumentNumber;
            ws.Cell(row, 5).Value = e.Description;
            ws.Cell(row, 6).Value = e.Amount;
            ws.Cell(row, 7).Value = e.ExcludeFromIpBox ? "TAK" : "NIE";
            row++;
        }

        wb.SaveAs(path);
    }

    public static void UpdateTemplateSafely(string templatePath, string outputPath, List<KpirEntry> entries)
    {
        File.Copy(templatePath, outputPath, true);

        using var wb = new XLWorkbook(outputPath);
        var ws = wb.Worksheet(1);

        var importedMonths = entries.Select(e => e.Date.Month).Distinct().OrderBy(m => m).ToList();
        if (importedMonths.Count == 0)
        {
            wb.Save();
            return;
        }

        var originalStarts = FindMonthStartRows(ws);
        var summaryStart = originalStarts[0];
        var originalBounds = BuildOriginalBounds(originalStarts, summaryStart);
        var offset = 0;

        foreach (var month in importedMonths)
        {
            if (!originalBounds.TryGetValue(month, out var bound))
                continue;

            var startRow = bound.start + offset;
            var endRow = bound.end + offset;

            var monthEntries = entries.Where(e => e.Date.Month == month).OrderBy(e => e.Date).ThenBy(e => e.Lp).ToList();
            var revenueEntries = monthEntries.Where(e => e.EntryType == KpirEntryType.RevenueIp).ToList();
            var costEntries = monthEntries.Where(e => e.EntryType == KpirEntryType.Cost).ToList();

            var currentRows = endRow - startRow + 1;
            var requiredRows = Math.Max(1, costEntries.Count);
            var diff = requiredRows - currentRows;

            var headerStyleRow = startRow;
            var detailStyleRow = Math.Min(startRow + 1, endRow);
            var lastStyleRow = endRow;

            if (diff > 0)
                ws.Row(endRow).InsertRowsBelow(diff);
            else if (diff < 0)
                ws.Rows(startRow + requiredRows, endRow).Delete();

            var newEndRow = startRow + requiredRows - 1;

            // Apply styles/row heights explicitly so inserted rows match template and bottom border stays only on last row.
            ApplyRowStyle(ws, headerStyleRow, startRow);
            if (requiredRows == 1)
            {
                ApplyRowStyle(ws, lastStyleRow + diff, startRow);
            }
            else
            {
                for (var r = startRow + 1; r < newEndRow; r++)
                    ApplyRowStyle(ws, detailStyleRow + Math.Max(diff, 0), r);

                ApplyRowStyle(ws, lastStyleRow + diff, newEndRow);
            }

            // Ensure merged month columns span the entire month block exactly once.
            ResetMonthMerges(ws, startRow, newEndRow);

            // Clear only user/data input cells. Keep template formulas in J and L.
            for (var r = startRow; r <= newEndRow; r++)
            {
                foreach (var c in new[] { 3, 4, 5, 6, 7, 8, 9, 11, 13, 18 })
                    ws.Cell(r, c).Clear(XLClearOptions.Contents);
            }

            // Header row data.
            ws.Cell(startRow, 3).Value = MonthNamesPl[month]; // C
            ws.Cell(startRow, 4).Clear(XLClearOptions.Contents); // D manual
            var revenueDocs = string.Join("; ", revenueEntries.Select(x => x.DocumentNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
            ws.Cell(startRow, 5).Value = revenueDocs; // E
            ws.Cell(startRow, 6).FormulaA1 = $"IFERROR(H{startRow}/G{startRow},\"\")"; // F
            var revenueAmount = revenueEntries.Sum(x => x.Amount);
            if (revenueAmount > 0)
                ws.Cell(startRow, 7).Value = revenueAmount; // G
            else
                ws.Cell(startRow, 7).Clear(XLClearOptions.Contents);
            ws.Cell(startRow, 8).Clear(XLClearOptions.Contents); // H manual
            ws.Cell(startRow, 12).FormulaA1 = $"=H{startRow}-SUM(J{startRow}:J{newEndRow})"; // L

            // Rebuild J formulas sequentially from the template pattern.
            // We keep the original absolute reference to F in the month header row,
            // but row-relative reference to I must advance with each detail row.
            for (var r = startRow; r <= newEndRow; r++)
            {
                ws.Cell(r, 10).FormulaA1 = $"=I{r}*$F${startRow}";
            }

            // Cost rows.
            for (var idx = 0; idx < requiredRows; idx++)
            {
                var row = startRow + idx;
                var cost = idx < costEntries.Count ? costEntries[idx] : null;

                if (cost != null)
                {
                    ws.Cell(row, 9).Value = cost.Amount; // I
                    // Keep template formula in J unchanged.
                    ws.Cell(row, 11).Value = BuildCostLabel(cost); // K
                    ws.Cell(row, 13).Value = cost.Amount; // M - koszty bezpośrednie
                    ws.Cell(row, 18).Value = cost.Lp; // R - numer w KPiR
                }
                else
                {
                    ws.Cell(row, 9).Clear(XLClearOptions.Contents);
                    ws.Cell(row, 11).Clear(XLClearOptions.Contents);
                    ws.Cell(row, 13).Clear(XLClearOptions.Contents);
                    ws.Cell(row, 18).Clear(XLClearOptions.Contents);
                }
            }

            offset += diff;
        }

        wb.Save();
    }

    private static Dictionary<int, int> FindMonthStartRows(IXLWorksheet ws)
    {
        var result = new Dictionary<int, int>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        for (var r = 1; r <= lastRow; r++)
        {
            var v = ws.Cell(r, 3).GetString().Trim();
            foreach (var kv in MonthNamesPl)
            {
                if (string.Equals(v, kv.Value, StringComparison.OrdinalIgnoreCase))
                {
                    result[kv.Key] = r;
                    break;
                }
            }

            if (string.Equals(v, "PODSUMOWANIE", StringComparison.OrdinalIgnoreCase))
                result[0] = r;
        }

        return result;
    }

    private static Dictionary<int, (int start, int end)> BuildOriginalBounds(Dictionary<int, int> starts, int summaryStart)
    {
        var ordered = starts.Where(kv => kv.Key >= 1 && kv.Key <= 12).OrderBy(kv => kv.Value).ToList();
        var result = new Dictionary<int, (int start, int end)>();

        for (var i = 0; i < ordered.Count; i++)
        {
            var start = ordered[i].Value;
            var end = i + 1 < ordered.Count ? ordered[i + 1].Value - 1 : summaryStart - 1;
            result[ordered[i].Key] = (start, end);
        }

        return result;
    }

    private static void ResetMonthMerges(IXLWorksheet ws, int startRow, int endRow)
    {
        var colsToMerge = new[] { 3, 4, 5, 6, 7, 8, 12 }; // C,D,E,F,G,H,L

        var intersecting = ws.MergedRanges
            .Where(r => r.RangeAddress.FirstAddress.RowNumber <= endRow &&
                        r.RangeAddress.LastAddress.RowNumber >= startRow &&
                        colsToMerge.Contains(r.RangeAddress.FirstAddress.ColumnNumber))
            .ToList();

        foreach (var r in intersecting)
            r.Unmerge();

        foreach (var col in colsToMerge)
        {
            if (endRow > startRow)
                ws.Range(startRow, col, endRow, col).Merge();
        }
    }

    private static void ApplyRowStyle(IXLWorksheet ws, int sourceRow, int targetRow)
    {
        ws.Row(targetRow).Height = ws.Row(sourceRow).Height;
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 18;

        for (var c = 1; c <= lastCol; c++)
        {
            var source = ws.Cell(sourceRow, c);
            var target = ws.Cell(targetRow, c);

            target.Style = source.Style;

            if (!string.IsNullOrWhiteSpace(source.FormulaA1))
            {
                // Copy formula in a way that preserves relative row references,
                // e.g. =I7*$F$3 -> =I8*$F$3 on the next inserted row.
                target.FormulaA1 = source.FormulaA1;
            }
        }
    }

    private static string BuildCostLabel(KpirEntry entry)
    {
        var description = entry.Description?.Trim() ?? "";
        var document = entry.DocumentNumber?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(description))
            return document;

        if (string.IsNullOrWhiteSpace(document))
            return description;

        return $"{description} ({document})";
    }
}
