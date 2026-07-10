using System;
using System.Collections.Generic;
#if WINDOWS
using System.Linq;
using OpenCvSharp;
#else
using System.IO;
using System.Text.Json;
#endif

namespace IpBoxKpirImporter.Services;

// Grid-line detection for KPiR table pages.
//   Windows  -> OpenCvSharp (native, unchanged behaviour).
//   macOS/Linux -> identical algorithm via pyhelper/detect_grid.py (Python + OpenCV).
// The backend is selected at build time by the WINDOWS constant (see .csproj).
public static class ImageGridDetector
{
    public sealed class GridLines
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<int> Horizontal { get; set; } = new();
        public List<int> Vertical { get; set; } = new();
    }

#if WINDOWS
    public static GridLines Detect(string imagePath)
    {
        using var src = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
        if (src.Empty())
            throw new InvalidOperationException($"Nie udało się otworzyć obrazu: {imagePath}");

        using var bw = new Mat();
        Cv2.Threshold(src, bw, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

        using var horizontal = bw.Clone();
        using var vertical = bw.Clone();

        var hKernelWidth = Math.Max(40, bw.Width / 18);
        var vKernelHeight = Math.Max(25, bw.Height / 45);

        using var hKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(hKernelWidth, 1));
        using var vKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, vKernelHeight));

        Cv2.MorphologyEx(horizontal, horizontal, MorphTypes.Open, hKernel);
        Cv2.MorphologyEx(vertical, vertical, MorphTypes.Open, vKernel);

        return new GridLines
        {
            Width = bw.Width,
            Height = bw.Height,
            Horizontal = Cluster(ProjectRows(horizontal), 6),
            Vertical = Cluster(ProjectCols(vertical), 8)
        };
    }

    private static List<int> ProjectRows(Mat mat)
    {
        var ys = new List<int>();
        for (var y = 0; y < mat.Rows; y++)
        {
            using var row = mat.Row(y);
            if (Cv2.CountNonZero(row) > mat.Cols * 0.35)
                ys.Add(y);
        }
        return ys;
    }

    private static List<int> ProjectCols(Mat mat)
    {
        var xs = new List<int>();
        for (var x = 0; x < mat.Cols; x++)
        {
            using var col = mat.Col(x);
            if (Cv2.CountNonZero(col) > mat.Rows * 0.15)
                xs.Add(x);
        }
        return xs;
    }

    private static List<int> Cluster(List<int> values, int tolerance)
    {
        var result = new List<int>();
        if (values.Count == 0) return result;

        var group = new List<int> { values[0] };
        for (var i = 1; i < values.Count; i++)
        {
            if (values[i] - group[^1] <= tolerance)
                group.Add(values[i]);
            else
            {
                result.Add((int)Math.Round(group.Average()));
                group = new List<int> { values[i] };
            }
        }
        result.Add((int)Math.Round(group.Average()));
        return result;
    }
#else
    private sealed class Payload
    {
        public int width { get; set; }
        public int height { get; set; }
        public List<int> horizontal { get; set; } = new();
        public List<int> vertical { get; set; } = new();
    }

    public static GridLines Detect(string imagePath)
    {
        var script = Path.Combine(AppContext.BaseDirectory, "pyhelper", "detect_grid.py");
        if (!File.Exists(script))
            script = Path.Combine(Directory.GetCurrentDirectory(), "pyhelper", "detect_grid.py");
        if (!File.Exists(script))
            throw new FileNotFoundException($"Nie znaleziono skryptu detektora siatki: {script}");

        var json = ProcessRunner.Run("python3", $"\"{script}\" \"{imagePath}\"");
        var p = JsonSerializer.Deserialize<Payload>(json)
                ?? throw new InvalidOperationException("Pusta odpowiedź detektora siatki.");

        return new GridLines
        {
            Width = p.width,
            Height = p.height,
            Horizontal = p.horizontal,
            Vertical = p.vertical
        };
    }
#endif
}
