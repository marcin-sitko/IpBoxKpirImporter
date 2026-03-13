
# IpBoxKpirImporter – Architecture

## Overview

Projekt parsuje pliki **PDF KPiR (InsERT nexo)** i aktualizuje arkusz **Ewidencja IP BOX** w Excelu.

Największe wyzwania:

- PDF nie zawiera prawdziwych tabel
- kwoty są podzielone na **złote i grosze**
- Excel template zawiera **merge cells, formuły i style**

Parser wykorzystuje **geometrię tabeli PDF**, a nie regexy ani OCR.

---

# Processing Pipeline

PDF
 ↓
PdfCellGridExtractor
 ↓
Table Cells
 ↓
Table Rows
 ↓
KpirRowMapper
 ↓
KpirEntry
 ↓
ExcelUpdater
 ↓
Ewidencja_IP_BOX.xlsx

---

# Components

## PdfCellGridExtractor

Odpowiedzialny za:

- wykrywanie linii tabeli
- budowanie siatki komórek
- ekstrakcję tekstu

Podejście:

PDF → linie tabeli → przecięcia → komórki → tekst

Dlaczego:

PDF zawiera tylko tekst z pozycją `(x,y)`, więc regex parsing jest niestabilny.

---

## Table Row Reconstruction

Po wykryciu komórek parser grupuje je w wiersze tabeli:

- LP
- data zdarzenia
- numer dokumentu
- opis
- kwoty

---

## KpirRowMapper

Mapper konwertuje wiersze tabeli na model:

KpirEntry

Model:

- Date
- Lp
- Document
- Description
- Amount
- Type (Income / Cost)

---

# Financial Amount Extraction

Kwoty w PDF są zapisane jako:

zł | gr

Przykład:

203 | 25

czyli:

203.25

Kolumny:

| PDF Column | Meaning |
|------------|--------|
| 9 | zł (przychód) |
| 10 | gr |
| 15 | zł (koszt) |
| 16 | gr |

Amount:

amount = zl + gr / 100

---

# Excel Update

## ExcelUpdater

Odpowiada za aktualizację template Excel.

Template zawiera:

- merge cells
- formuły
- style
- border oddzielające miesiące

Dlatego:

- style są kopiowane z wierszy referencyjnych
- merge cells są zachowywane
- formuły nie są nadpisywane

---

# Debug Mode

Program obsługuje flagę:

--debug

Generowane są pliki:

*_cellgrid_lines
*_cellgrid_rows

Pozwala to analizować nowe layouty PDF.

---

# Tests

Referencyjny PDF:

test-data/KPiR 01_2026.pdf

Test sprawdza:

- liczbę wierszy tabeli
- liczbę wpisów
- sekwencję LP

Uruchomienie:

dotnet test

---

# Monthly Workflow

1. eksport KPiR z InsERT
2. uruchomienie programu
3. generowane pliki:

kpir_import.xlsx
Ewidencja_IP_BOX_z_importem.xlsx

4. ręczne uzupełnienie:

- kolumna H
- przychód kwalifikowany IP

---

# Design Decisions

## No OCR

PDF z InsERT zawiera warstwę tekstową, OCR generował błędy.

## No Regex Parsing

Regex jest niestabilny przy zmianie layoutu.

Zastosowano:

PDF geometry parsing

---

# Future Improvements

- automatyczne wykrywanie roku
- lepsze logowanie
- eksport debug JSON
- CLI arguments
