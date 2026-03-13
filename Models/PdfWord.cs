namespace IpBoxKpirImporter.Models;

public sealed class PdfWord
{
    public string Text { get; set; } = "";
    public double PxX0 { get; set; }
    public double PxX1 { get; set; }
    public double PxY0 { get; set; }
    public double PxY1 { get; set; }

    public double CenterPxX => (PxX0 + PxX1) / 2.0;
    public double CenterPxY => (PxY0 + PxY1) / 2.0;
}
