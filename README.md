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
