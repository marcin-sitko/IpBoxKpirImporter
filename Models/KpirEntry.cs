using System;

namespace IpBoxKpirImporter.Models;

public enum KpirEntryType
{
    Cost,
    RevenueIp,
    RevenueOther
}

public sealed class KpirEntry
{
    public int Lp { get; set; }
    public DateTime Date { get; set; }
    public string MonthNamePl { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public KpirEntryType EntryType { get; set; }
    public bool ExcludeFromIpBox { get; set; }
}
