
# IpBoxKpirImporter – Project State

## Purpose
Projekt automatyzuje import danych z PDF KPiR (InsERT nexo) do arkusza Excel Ewidencja IP BOX.

Proces:
1. Parsowanie tabeli z PDF
2. Mapowanie wierszy na model danych
3. Generowanie kpir_import.xlsx
4. Aktualizacja template Ewidencja_IP_BOX.xlsx bez psucia layoutu

---

## Architecture

Pipeline:

PDF → PdfCellGridExtractor → Table rows → KpirRowMapper → KpirEntry → ExcelUpdater → Excel

---

## Components

### PdfCellGridExtractor
Wykrywa linie tabeli w PDF i buduje siatkę komórek.
Parser nie używa OCR.

### KpirRowMapper
Mapuje wiersze tabeli na model KpirEntry.

### ExcelUpdater
Aktualizuje template Excel zachowując:
- merge cells
- formuły
- style
- border miesięcy

---

## Financial Columns

PDF:

9  = zł (przychód)  
10 = gr  
15 = zł (koszt)  
16 = gr  

Kwota:

amount = zl + gr / 100

---

## Reference Test

Folder:

test-data/KPiR 01_2026.pdf

Uruchamianie:

dotnet test

---

## Debug Mode

Flaga:

--debug

Generuje pliki diagnostyczne:

*_cellgrid_lines
*_cellgrid_rows

---

## Typical Workflow

1 eksport KPiR z InsERT  
2 uruchomienie programu  
3 powstają pliki:

kpir_import.xlsx  
Ewidencja_IP_BOX_z_importem.xlsx

4 ręczne uzupełnienie kolumny H
