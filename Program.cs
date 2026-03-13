using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IpBoxKpirImporter.Models;
using IpBoxKpirImporter.Services;

if (args.Length < 3)
{
    Console.WriteLine("Usage: IpBoxKpirImporter <kpir_folder> <excel_template> <output_folder> [--debug]");
    return;
}

var kpirFolder = args[0];
var excelTemplate = args[1];
var outputFolder = args[2];
var debugEnabled = args.Any(a => string.Equals(a, "--debug", StringComparison.OrdinalIgnoreCase));

Directory.CreateDirectory(outputFolder);

Console.WriteLine("[1/4] Wykrywam siatkę komórek i odczytuję tabelę KPiR z PDF...");
var extractor = new PdfCellGridExtractor(outputFolder, debugEnabled);
var tableRows = new List<KpirTableRow>();

foreach (var file in Directory.GetFiles(kpirFolder, "*.pdf").OrderBy(x => x))
{
    try
    {
        var rows = extractor.Extract(file);
        Console.WriteLine($"[INFO] {Path.GetFileName(file)} -> {rows.Count} wierszy tabeli");
        tableRows.AddRange(rows);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {Path.GetFileName(file)} -> {ex.Message}");
    }
}

Console.WriteLine("[2/4] Mapuję wiersze tabeli do modelu biznesowego...");
var mapper = new KpirRowMapper();
var entries = tableRows
    .Select(mapper.Map)
    .Where(x => x != null)
    .Cast<KpirEntry>()
    .OrderBy(x => x.Date)
    .ThenBy(x => x.Lp)
    .ToList();

Console.WriteLine("[3/4] Zapis importu...");
var importPath = Path.Combine(outputFolder, "kpir_import.xlsx");
ExcelUpdater.WriteImportWorkbook(importPath, entries);

Console.WriteLine("[4/4] Aktualizuję skoroszyt IP Box...");
var outputWorkbook = Path.Combine(outputFolder, "Ewidencja_IP_BOX_z_importem.xlsx");
ExcelUpdater.UpdateTemplateSafely(excelTemplate, outputWorkbook, entries);

Console.WriteLine();
Console.WriteLine("Gotowe:");
Console.WriteLine(importPath);
Console.WriteLine(outputWorkbook);
if (debugEnabled)
    Console.WriteLine("Pliki debug zapisane w folderze wyjściowym.");
