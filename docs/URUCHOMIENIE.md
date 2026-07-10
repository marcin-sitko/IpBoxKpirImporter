# Uruchomienie IpBoxKpirImporter

Konsolowa aplikacja .NET 8, która czyta PDF-y KPiR (eksport z InsERT nexo) i wypełnia miesięczne bloki szablonu Excel „Ewidencja IP BOX”, zachowując scalenia, formuły i style.

## Co robi program

```
PDF KPiR → PdfCellGridExtractor → wiersze tabeli → KpirRowMapper → KpirEntry → ExcelUpdater → Excel
```

1. Renderuje strony PDF do PNG (`pdftoppm`) i wykrywa siatkę tabeli (OpenCV).
2. Odczytuje warstwę tekstową PDF (PdfPig) i przypisuje słowa do komórek siatki.
3. Mapuje wiersze na model `KpirEntry` (data, Lp, dokument, opis, kwota, typ: przychód IP / przychód pozostały / koszt), składając kwoty z par zł/gr.
4. Zapisuje `kpir_import.xlsx` (płaska lista) i aktualizuje szablon → `Ewidencja_IP_BOX_z_importem.xlsx`.

## Wymagania

- **.NET 8 SDK** (`dotnet --version` ≥ 8).
- **Poppler** – narzędzie `pdftoppm` musi być dostępne w `PATH`. Program wywołuje je przez `ProcessRunner`; bez niego ekstrakcja PDF nie ruszy.
  - Windows: pobierz Poppler for Windows, rozpakuj i dodaj folder `bin` do zmiennej `PATH`.
  - Linux/macOS: `apt install poppler-utils` / `brew install poppler`.
- **System operacyjny: Windows.** Projekt referuje `OpenCvSharp4.runtime.win` (natywne biblioteki OpenCV tylko dla Windows). Aby zbudować/uruchomić na Linux lub macOS, trzeba podmienić tę paczkę na odpowiedni runtime, np. `OpenCvSharp4.runtime.linux` lub `OpenCvSharp4.runtime.osx` w `IpBoxKpirImporter.csproj`.

Zależności NuGet (pobierają się automatycznie przy `dotnet restore`): ClosedXML 0.104.2, PdfPig 0.1.13, OpenCvSharp4 4.10.

## Szybki start na macOS / Linux

Projekt działa też poza Windows — fragment wykrywania siatki tabeli używa wtedy backendu w Pythonie (`pyhelper/detect_grid.py`), a build automatycznie pomija OpenCvSharp. Reszta logiki jest identyczna.

Instalacja jednorazowa:

```bash
brew install dotnet@8 poppler
pip3 install opencv-python-headless
```

Comiesięczne generowanie:

1. Wrzuć nowy PDF od księgowej do folderu `KPIR/` (mają tam być wszystkie miesiące roku, które trafią do ewidencji).
2. Uruchom:

```bash
./run.sh
```

albo dwuklik na `run.command` w Finderze. Skrypt sam sprawdza zależności, przyjmuje domyślnie `KPIR/` → `templates/Ewidencja_IP_BOX-wzor.xlsx` → `out/` i wypisuje ścieżkę wyniku. Nazwę zakładki w wynikowym pliku (`Ewidencja_IP_BOX`) możesz zmienić na dany rok ręcznie.

Własne ścieżki: `./run.sh <folder_pdf> <szablon_xlsx> <folder_out>`.

## Build

```powershell
dotnet restore IpBoxKpirImporter.sln
dotnet build IpBoxKpirImporter.sln -c Release
```

Albo otwórz `IpBoxKpirImporter.sln` w Visual Studio i zbuduj rozwiązanie.

## Uruchomienie

```
IpBoxKpirImporter <folder_z_pdf_kpir> <szablon_excel> <folder_wyjsciowy> [--debug]
```

Przez `dotnet run`:

```powershell
dotnet run --project IpBoxKpirImporter.csproj -- ".\kpir" ".\templates\Ewidencja_IP_BOX-wzor.xlsx" ".\out"
```

Argumenty:

- `folder_z_pdf_kpir` – katalog z plikami `*.pdf` (przetwarzane alfabetycznie). Przykładowe PDF-y: `test-data/KPiR 01_2026.pdf`, `test-data/KPiR 02_2026.pdf`.
- `szablon_excel` – plik szablonu ewidencji z blokami miesięcznymi i wierszem `PODSUMOWANIE`. Wzór: `templates/Ewidencja_IP_BOX-wzor.xlsx` – **pusty, wielorazowy szablon** (struktura, style, scalenia, formuły i wskaźnik nexus zachowane; bez danych rocznych). Używaj go jako stałego wejścia; pola ręczne (opis projektu, czas, przychód z kwalifikowanego IP) uzupełniasz po imporcie.
- `folder_wyjsciowy` – tworzony automatycznie; trafiają tu wyniki.
- `--debug` (opcjonalnie) – zapisuje pliki diagnostyczne `*_cellgrid_lines_*` i `*_cellgrid_rows_*` do folderu wyjściowego.

### Pliki wynikowe

- `kpir_import.xlsx` – płaska lista wpisów (Lp, Date, Type, Document, Description, Amount, Excluded).
- `Ewidencja_IP_BOX_z_importem.xlsx` – kopia szablonu z wypełnionymi blokami miesięcy.

Program dopasowuje bloki miesięcy po nazwie w kolumnie C, wstawia/usuwa wiersze pod liczbę kosztów w miesiącu, odtwarza scalenia, style i formuły, po czym wpełnia:

| Kolumna | Zawartość |
|---------|-----------|
| C | nazwa miesiąca |
| E | numery faktur przychodowych |
| F | `=H/G` (% kwalifikowany) |
| G | przychód całkowity |
| I | kwota kosztu z KPiR |
| J | `=I*$F$` (kwota IP Box) |
| K | rodzaj/opis kosztu |
| L | `=H-SUM(J:J)` (dochód) |
| M | koszty bezpośrednie (kopia kwoty kosztu) |
| R | Lp z KPiR |

## Krok ręczny po imporcie

Importer realizuje tylko krok 1 z instrukcji (wprowadzenie kosztów i przychodów). Ręcznie uzupełnia się m.in. kolumnę H (przychód z kwalifikowanego IP), podział na projekty oraz weryfikację kosztów bezpośrednich (zerowanie). Reguły: [`ip-box-ewidencja-instrukcja.md`](ip-box-ewidencja-instrukcja.md).

## Testy

Test integracyjny (xUnit) sprawdza liczbę wierszy, liczbę wpisów i sekwencję Lp na referencyjnym PDF:

```powershell
dotnet test IpBoxKpirImporter.sln
```

Oczekiwane wartości: `test-data/expected.json`. Skrypty pomocnicze: `tools/run-smoke-test.ps1`, `tools/run-debug-example.ps1`.

## Typowe problemy

- **„Nie udało się uruchomić procesu: pdftoppm”** – Poppler nie jest w `PATH`.
- **Wyjątek OpenCV / brak natywnej biblioteki** – uruchamiasz poza Windows albo brakuje pasującej paczki `OpenCvSharp4.runtime.*`.
- **Kwoty = 0 lub pominięte wiersze** – nietypowy layout PDF; uruchom z `--debug` i porównaj pliki `*_cellgrid_*` z siatką tabeli.
- **Rozjechane bloki miesięcy** – szablon musi mieć nazwy miesięcy w kolumnie C i wiersz `PODSUMOWANIE` (patrz `templates/Ewidencja_IP_BOX-wzor.xlsx`).
