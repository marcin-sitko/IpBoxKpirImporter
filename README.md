# IpBoxKpirImporter

Import danych z PDF KPiR (InsERT nexo) do szablonu Excel „Ewidencja IP BOX”.

**Dokumentacja:**
- [docs/URUCHOMIENIE.md](docs/URUCHOMIENIE.md) – wymagania, build, uruchomienie, testy, mapowanie kolumn.
- [docs/ip-box-ewidencja-instrukcja.md](docs/ip-box-ewidencja-instrukcja.md) – reguły biznesowe IP Box (transkrypcja materiałów źródłowych).
- [ARCHITECTURE.md](ARCHITECTURE.md) · [PROJECT_STATE.md](PROJECT_STATE.md) · [CHANGELOG.md](CHANGELOG.md)
- Materiały źródłowe: [docs/uzupelnianie-ewidencji.pdf](docs/uzupelnianie-ewidencji.pdf), [docs/koszty.docx](docs/koszty.docx)
- Szablon wejściowy: [templates/Ewidencja_IP_BOX-wzor.xlsx](templates/Ewidencja_IP_BOX-wzor.xlsx) · PDF-y testowe: `test-data/`

---

## Historia zmian parsera

Cell-grid v2:
- strony bez pionowych linii dziedziczą układ kolumn z poprzedniej strony PDF,
- słowa są przypisywane do komórek po maksymalnym overlapie poziomym, nie tylko po środku,
- mapper nadal bierze kwoty wyłącznie z kolumn tabeli.

To ma naprawić:
- znikający LP 14 na drugiej stronie,
- zera w Amount spowodowane złym przypisaniem słów do sąsiednich komórek,
- mniejsze rozjazdy dokumentów/opisów przy granicach kolumn.


v3 poprawki:
- kwoty są teraz liczone z par rozbitych komórek zł/gr:
  - przychód: 9 + 10
  - koszt: 15+16, 14+15, 13+14, 12+13
- grosze typu "25" i złotówki typu "203" są składane w jedną kwotę 203.25,
- data jest obcinana do pierwszego poprawnego dd-MM-yyyy, żeby nie łapać śmieci obok.


v4 poprawki:
- przychody nadal są liczone z 9 + 10,
- koszty są teraz priorytetowo liczone jako para zł/gr z kolumn odpowiadających drukowanej kolumnie 15:
  - 15 + 16
  - 14 + 15
  - 13 + 14
  - 12 + 13
- dzięki temu wpisy typu:
  - 44 | 53
  - 1 171 | 41
  - 345 | 00
  - 167 | 28
są składane do poprawnych kwot kosztowych.


v5 poprawki:
- dla kosztów dodany fallback do prawej, końcowej zdublowanej kwoty w FullRowText,
- przykłady obsługiwane przez fallback:
  - "1 185 54 1 185 54" -> 1185.54
  - "405 73 405 73" -> 405.73
  - "138 47 138 47" -> 138.47
  - "345 00 345 00" -> 345.00
- przychody nadal liczone z 9 + 10.


v8 updater:
- aktualizacja template działa na oryginalnych blokach miesięcznych z zachowaniem przesunięć kolejnych miesięcy,
- granice miesięcy liczone są względem oryginalnego pliku i korygowane kumulacyjnym offsetem,
- po wstawieniu/usunięciu wierszy style są ponownie nakładane z wzorca:
  - pierwszy wiersz miesiąca,
  - środkowy wiersz detali,
  - ostatni wiersz miesiąca
- to ma usunąć:
  - rozjechane puste wiersze,
  - błędne grube dolne obramowania,
  - dublowanie / nakładanie się kolejnych miesięcy.


v9 updater:
- po zmianie liczby wierszy w miesiącu updater jawnie resetuje i odtwarza scalone kolumny C,D,E,F,G,H,L dla całego bloku miesiąca,
- dzięki temu nowe wiersze nie rozbijają merged cells miesiąca,
- nagłówek miesiąca (nazwa, faktura, przychód, formuły) zostaje tylko w pierwszym wierszu bloku,
- L zawsze liczy SUM(J[start]:J[end]) dla aktualnej liczby wierszy miesiąca.


v10 updater:
- wypełnia dodatkowo:
  - M = wartość z I (koszty bezpośrednie)
  - R = LP / numer w KPiR
- nie nadpisuje formuł w J ani F; zachowuje formuły z template,
- przy kopiowaniu wierszy przenosi też formuły z wiersza wzorcowego, nie tylko style,
- czyści tylko pola danych wejściowych, nie usuwa formuł template.


v12 cleanup:
- dodana flaga --debug; bez niej nie zapisuje plików debug,
- ExcelUpdater posprzątany:
  - osobne metody do header/costs/clear/merges/restyle,
  - sekwencyjne formuły J odbudowywane jawnie,
  - F i L ustawiane tylko raz na miesiąc,
- poprawione style:
  - wewnętrzne wiersze miesiąca mają usuwany dolny gruby border,
  - gruby dolny border zostaje tylko na ostatnim wierszu miesiąca.


v13:
- fix kompilacji CS1729:
  PdfCellGridExtractor ma znowu konstruktor:
  PdfCellGridExtractor(string debugFolder, bool debugEnabled = false)


v14:
- fix borderów: nie kasuje już dolnego borderu w wierszach wewnętrznych,
- style wierszy są kopiowane dokładnie z odpowiedniego wiersza wzorcowego:
  - header -> pierwszy wiersz miesiąca
  - detail -> wiersze środkowe
  - footer -> ostatni wiersz miesiąca
- to naprawia brak dolnych borderów typu J15:J22.


v15:
- dodany folder `test-data/` z referencyjnym PDF:
  - `KPiR 01_2026.pdf`
- dodany prosty test integracyjny:
  - projekt `SmokeTests`
  - sprawdza liczbę wierszy tabeli, liczbę wpisów i sekwencję LP
- flaga `--debug` zostaje w głównym programie:
  - bez flagi nie zapisuje plików debug
  - z flagą zapisuje pliki debug do folderu wyjściowego

Uruchamianie:
- test integracyjny:
  - `powershell -ExecutionPolicy Bypass -File .\tools\run-smoke-test.ps1`
- przykład uruchomienia z debug:
  - `powershell -ExecutionPolicy Bypass -File .\tools\run-debug-example.ps1`


v16:
- wszystko zostało przeniesione do katalogu `IpBoxKpirImporter/`, żeby pasowało do struktury repo:
  - `IpBoxKpirImporter/SmokeTests`
  - `IpBoxKpirImporter/test-data`
  - `IpBoxKpirImporter/tools`
- poprawione ścieżki w testach i skryptach po przeniesieniu.


v17:
- test integracyjny został przeniesiony na xUnit, żeby dało się go uruchamiać bezpośrednio z Visual Studio / Test Explorer
- dodane:
  - `IpBoxKpirImporter.sln`
  - `IpBoxKpirImporter.Tests/`
- referencyjny PDF dalej siedzi w:
  - `test-data/KPiR 01_2026.pdf`

Uruchamianie w Visual Studio:
1. Otwórz `IpBoxKpirImporter.sln`
2. Build solution
3. Test Explorer -> Run All

Uruchamianie z CLI:
- `dotnet test IpBoxKpirImporter.sln`


v18:
- fix dla xUnit / Visual Studio:
  - główny projekt `IpBoxKpirImporter.csproj` wyklucza teraz folder `IpBoxKpirImporter.Tests`
  - to usuwa błędy:
    - duplicate assembly attributes
    - missing `Fact` / `Xunit`
    - missing `IpBoxKpirImporter.dll`
- przyczyną było to, że pliki testów siedziały pod katalogiem głównego projektu i były przez SDK automatycznie wciągane do kompilacji aplikacji.
