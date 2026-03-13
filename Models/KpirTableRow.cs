using System.Collections.Generic;

namespace IpBoxKpirImporter.Models;

public sealed class KpirTableRow
{
    public int PageNumber { get; set; }
    public int RowIndexOnPage { get; set; }

    public string Lp { get; set; } = "";
    public string Date { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Counterparty { get; set; } = "";
    public string Description { get; set; } = "";

    public string RevenueGoodsAndServices { get; set; } = "";
    public string RevenueOther { get; set; } = "";
    public string RevenueTotal { get; set; } = "";

    public string PurchaseGoods { get; set; } = "";
    public string IncidentalPurchaseCosts { get; set; } = "";
    public string SalaryInCash { get; set; } = "";
    public string OtherExpense { get; set; } = "";
    public string ExpenseTotal { get; set; } = "";

    public string Notes { get; set; } = "";
    public string FullRowText { get; set; } = "";

    public Dictionary<int, string> RawByColumn { get; set; } = new();
}
