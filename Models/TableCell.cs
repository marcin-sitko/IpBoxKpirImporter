namespace IpBoxKpirImporter.Models;

public sealed class TableCell
{
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public double Left { get; set; }
    public double Right { get; set; }
    public double Top { get; set; }
    public double Bottom { get; set; }

    public bool ContainsCenter(double x, double y) => x >= Left && x < Right && y >= Top && y < Bottom;

    public double OverlapWidth(double x0, double x1)
    {
        var left = x0 > Left ? x0 : Left;
        var right = x1 < Right ? x1 : Right;
        return right > left ? right - left : 0.0;
    }
}
