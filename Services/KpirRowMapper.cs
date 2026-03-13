using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using IpBoxKpirImporter.Models;

namespace IpBoxKpirImporter.Services;

public sealed class KpirRowMapper
{
    private static readonly Regex DateRegex = new(@"\b\d{2}-\d{2}-\d{4}\b");

    public KpirEntry? Map(KpirTableRow row)
    {
        if (!int.TryParse(ExtractFirstInteger(row.Lp), out var lp) || lp <= 0 || lp > 500)
            return null;

        var dateText = FirstNonEmpty(ExtractDate(row.Date), ExtractDate(row.FullRowText));
        if (!DateTime.TryParseExact(dateText, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;

        var description = NormalizeCell(row.Description);
        var document = NormalizeDocument(row.DocumentNumber);

        var entryType = ResolveEntryType(row);
        var amount = ResolveAmount(row, entryType);

        return new KpirEntry
        {
            Lp = lp,
            Date = date,
            MonthNamePl = date.ToString("MMMM", new CultureInfo("pl-PL")),
            DocumentNumber = document,
            Description = description,
            Amount = amount,
            EntryType = entryType,
            ExcludeFromIpBox = IsParkingRevenue(row)
        };
    }

    private static KpirEntryType ResolveEntryType(KpirTableRow row)
    {
        var revenue = ResolveRevenueAmount(row);
        if (revenue > 0m)
            return IsParkingRevenue(row) ? KpirEntryType.RevenueOther : KpirEntryType.RevenueIp;

        return KpirEntryType.Cost;
    }

    private static bool IsParkingRevenue(KpirTableRow row)
    {
        var text = ((row.Description ?? "") + " " + (row.FullRowText ?? "")).ToLowerInvariant();
        return text.Contains("wynajem miejsca postojowego");
    }

    private static decimal ResolveAmount(KpirTableRow row, KpirEntryType type)
    {
        return type == KpirEntryType.RevenueIp || type == KpirEntryType.RevenueOther
            ? ResolveRevenueAmount(row)
            : ResolveCostAmount(row);
    }

    private static decimal ResolveRevenueAmount(KpirTableRow row)
    {
        var pair910 = ParseSplitMoney(row.RevenueGoodsAndServices, row.RevenueOther);
        if (pair910 > 0) return pair910;

        var total = ParseMoney(row.RevenueTotal);
        if (total > 0) return total;

        var goods = ParseMoney(row.RevenueGoodsAndServices);
        if (goods > 0) return goods;

        var other = ParseMoney(row.RevenueOther);
        if (other > 0) return other;

        return 0m;
    }

    private static decimal ResolveCostAmount(KpirTableRow row)
    {
        // First try explicit column pairs if they were extracted.
        var pair1516 = ParseSplitMoney(row.OtherExpense, row.ExpenseTotal);
        if (pair1516 > 0) return pair1516;

        var pair1415 = ParseSplitMoney(row.SalaryInCash, row.OtherExpense);
        if (pair1415 > 0) return pair1415;

        var pair1314 = ParseSplitMoney(row.IncidentalPurchaseCosts, row.SalaryInCash);
        if (pair1314 > 0) return pair1314;

        var pair1213 = ParseSplitMoney(row.PurchaseGoods, row.IncidentalPurchaseCosts);
        if (pair1213 > 0) return pair1213;

        var total = ParseMoney(row.ExpenseTotal);
        if (total > 0) return total;

        var other = ParseMoney(row.OtherExpense);
        if (other > 0) return other;

        var salary = ParseMoney(row.SalaryInCash);
        if (salary > 0) return salary;

        var incidental = ParseMoney(row.IncidentalPurchaseCosts);
        if (incidental > 0) return incidental;

        var purchase = ParseMoney(row.PurchaseGoods);
        if (purchase > 0) return purchase;

        // Fallback for these PDFs: final cost is often printed as the rightmost duplicated amount in the full row.
        var fullDup = ParseRightmostDuplicatedMoney(row.FullRowText);
        if (fullDup > 0) return fullDup;

        return 0m;
    }

    private static decimal ParseSplitMoney(string left, string right)
    {
        var zl = ExtractIntegerValue(left);
        var gr = ExtractGrosze(right);

        if (zl.HasValue && gr.HasValue)
            return decimal.Parse($"{zl.Value}.{gr.Value:00}", CultureInfo.InvariantCulture);

        var fullLeft = ParseMoney(left);
        if (fullLeft > 0) return fullLeft;

        return 0m;
    }

    private static int? ExtractIntegerValue(string text)
    {
        text = NormalizeCell(text);
        if (string.IsNullOrWhiteSpace(text)) return null;

        var dup = Regex.Match(text, @"(?<!\d)(\d{1,3}(?: \d{3})*|\d+)\s+\1(?!\d)");
        if (dup.Success)
            return int.Parse(dup.Groups[1].Value.Replace(" ", ""), CultureInfo.InvariantCulture);

        var m = Regex.Match(text, @"(?<!\d)(\d{1,3}(?: \d{3})*|\d+)(?!\d)");
        if (m.Success)
            return int.Parse(m.Groups[1].Value.Replace(" ", ""), CultureInfo.InvariantCulture);

        return null;
    }

    private static int? ExtractGrosze(string text)
    {
        text = NormalizeCell(text);
        if (string.IsNullOrWhiteSpace(text)) return null;

        var dup = Regex.Match(text, @"(?<!\d)(\d{2})\s+\1(?!\d)");
        if (dup.Success)
            return int.Parse(dup.Groups[1].Value, CultureInfo.InvariantCulture);

        var m = Regex.Match(text, @"(?<!\d)(\d{2})(?!\d)");
        if (m.Success)
            return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);

        return null;
    }

    private static decimal ParseMoney(string text)
    {
        text = NormalizeCell(text);
        if (string.IsNullOrWhiteSpace(text))
            return 0m;

        var dup = Regex.Match(text, @"(?<!\d)(\d{1,3}(?: \d{3})*|\d+)\s+(\d{2})\s+\1\s+\2(?!\d)");
        if (dup.Success)
        {
            var integerPart = dup.Groups[1].Value.Replace(" ", "");
            return decimal.Parse($"{integerPart}.{dup.Groups[2].Value}", CultureInfo.InvariantCulture);
        }

        var pair = Regex.Match(text, @"(?<!\d)(\d{1,3}(?: \d{3})*|\d+)\s+(\d{2})(?!\d)");
        if (pair.Success)
        {
            var integerPart = pair.Groups[1].Value.Replace(" ", "");
            return decimal.Parse($"{integerPart}.{pair.Groups[2].Value}", CultureInfo.InvariantCulture);
        }

        return 0m;
    }

    private static decimal ParseRightmostDuplicatedMoney(string text)
    {
        text = NormalizeCell(text);
        if (string.IsNullOrWhiteSpace(text))
            return 0m;

        var matches = Regex.Matches(
            text,
            @"(?<!\d)(\d{1,3}(?: \d{3})*|\d+)\s+(\d{2})\s+\1\s+\2(?!\d)");

        if (matches.Count == 0)
            return 0m;

        var m = matches[matches.Count - 1];
        var integerPart = m.Groups[1].Value.Replace(" ", "");
        return decimal.Parse($"{integerPart}.{m.Groups[2].Value}", CultureInfo.InvariantCulture);
    }

    private static string NormalizeDocument(string text)
    {
        text = NormalizeCell(text);
        if (string.IsNullOrWhiteSpace(text))
            return text;

        text = Regex.Replace(text, @"(?<=/\d{2}/\d{3})\s+(?=\d\b)", "");
        text = Regex.Replace(text, @"\s*/\s*", "/");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    private static string FirstNonEmpty(params string[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";

    private static string ExtractDate(string text)
    {
        var m = DateRegex.Match(text ?? "");
        return m.Success ? m.Value : "";
    }

    private static string ExtractFirstInteger(string text)
    {
        var m = Regex.Match(text ?? "", @"\b\d{1,3}\b");
        return m.Success ? m.Value : "";
    }

    private static string NormalizeCell(string text) => Regex.Replace(text ?? "", @"\s+", " ").Trim();
}
