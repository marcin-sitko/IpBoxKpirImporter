# IP Box – reguły prowadzenia Ewidencji

Transkrypcja i podsumowanie materiałów źródłowych dołączonych do repo:

- [`uzupelnianie-ewidencji.pdf`](uzupelnianie-ewidencji.pdf) – schemat pracy z ewidencją,
- [`koszty.docx`](koszty.docx) – definicje i klasyfikacja kosztów.

Ten plik jest wersją tekstową tych dokumentów, żeby reguły były wersjonowane w gicie i czytelne z kodu. W razie rozbieżności źródłem prawdy pozostają oryginały.

---

## Czym jest ewidencja

Ewidencja IP Box służy do ustalenia przychodów i kosztów z danego projektu (kwalifikowanego IP) oraz do obliczenia **wskaźnika nexus**. Jest dokumentem wymaganym przez urząd i weryfikowanym na etapie czynności sprawdzających.

Przesłany plik `Ewidencja` to tylko przykład. Można dodawać kolumny i wiersze oraz modyfikować reguły pod konkretny przypadek. **Nie wolno usuwać kolumn** – nawet jeśli po uzupełnieniu pozostaną puste.

---

## Schemat pracy z ewidencją (roczny)

1. **Wprowadzamy koszty.** Dla każdego miesiąca wprowadzamy koszty zgodnie z wartościami z miesięcznych zestawień KPiR. Po wprowadzeniu miesiąca sprawdzamy, czy suma kosztów wprowadzonych nie jest wyższa niż suma kosztów w KPiR. Uzupełniamy numer z KPiR oraz liczbę porządkową (Lp.) dla danego dokumentu.
2. **Ustalamy liczbę projektów** prowadzonych w danym roku i w jakich miesiącach.
3. **Kopiujemy tabelę z kosztami** tyle razy, ile projektów prowadzono w roku.
4. **Uzupełniamy** numer projektu, opis i czas poświęcony na pracę.
5. **Porządkujemy koszty.** Dla każdego projektu zostawiamy tylko miesiące, w których nad nim pracowano; pozostałe miesiące usuwamy z tego projektu całkowicie (np. projekt marzec–kwiecień → zostają tylko marzec i kwiecień).
6. **Uzupełniamy przychody:**
   - Przychód całkowity = suma wszystkich przychodów z KPiR dla danego miesiąca.
   - Przychód z kwalifikowanego IP = przychód przypadający na dany projekt w miesiącu.
   - Udział procentowy = przychód z kwalifikowanego IP / przychód całkowity.
7. **Koszty przypadające na projekt** = koszty z KPiR × udział procentowy dla danego projektu.
8. **Dochód z miesiąca** = przychód przypadający na projekt − suma kosztów przypadających na projekt (pkt 7).
9. **Koszty bezpośrednie** – kopiujemy koszty przypadające na kwalifikowane IP (pkt 7) i weryfikujemy je wg zasad z sekcji „Koszty bezpośrednie”.

---

## Rodzaje kosztów

W IP Box wyróżniamy dwa rodzaje kosztów:

### Koszty uzyskania przychodu (kolumny I, J, K w przykładzie)

Służą do wyliczenia dochodu z kwalifikowanego IP. Przy działalności jednokierunkowej (brak przychodów z innych źródeł niż programistyczne) w koszty uzyskania przychodu wpisujemy **wszystkie** koszty z KPiR, w kwotach tam wykazanych (kolumna I). Jeśli w danym miesiącu udział przychodu z projektu jest niższy niż 100% (kilka projektów), trzeba wyliczyć, jaka część kosztu przypada na dany projekt (patrz Przykład 1).

### Koszty bezpośrednie (kolumna M)

Służą do wyliczenia wskaźnika nexus. **Każdy koszt bezpośredni jest kosztem uzyskania przychodu, ale nie każdy koszt uzyskania przychodu jest kosztem bezpośrednim.** Po ustaleniu kosztów uzyskania przychodu przypadających na projekt kopiujemy je do kolumny bezpośredniej i weryfikujemy.

**Do wyzerowania w kolumnie „koszty bezpośrednie” są:**

- koszty związane z nieruchomościami (czynsz, energia itp.),
- koszty tzw. pozostałe (materiały gospodarcze, koszty socjalne – woda, cukierki dla klientów),
- koszty finansowe (ujemne różnice kursowe, odsetki bankowe, odsetki za nieterminową wpłatę, opłaty skarbowe – jeśli są KUP i są w KPiR),
- koszty „na potrzeby działalności” (odkurzacz, ekspres do kawy itp.).

---

## Przypadki szczególne

**Składki ZUS.** Dwa warianty rozliczenia:

- ZUS odliczany od dochodu (wykazywany dopiero w PIT-36/PIT-36L, niewidoczny jako koszt w KPiR) – **nie** wykazujemy w ewidencji.
- ZUS zaliczany do kosztów uzyskania przychodu (widoczny w KPiR) – stanowi KUP i **powinien** być wykazany w ewidencji.

**Różnice kursowe od faktur sprzedaży związanych z IP Box:**

- dodatnie (pozostały przychód, 8. kolumna KPiR) → przychód z kwalifikowanego IP,
- ujemne (koszt, 13. kolumna KPiR) → KUP w ewidencji; **zerujemy** je w kolumnie koszty bezpośrednie.

---

## Przykład 1 – podział kosztów przy kilku projektach

W lutym: przychód 10 000 zł, koszty 5 000 zł. Przychód pochodzi z:

- a) sprzedaż samochodu z działalności – 2 000 zł,
- b) sprzedaż praw do programu Alfa – 3 000 zł,
- c) sprzedaż praw do programu Beta – 5 000 zł.

Do ewidencji wprowadzamy tylko przychody ze sprzedaży programów (b i c). Udziały w przychodzie ogółem: a) 20%, b) 30%, c) 50%. Koszty przypisujemy proporcjonalnie:

- a) samochodu nie wpisujemy do ewidencji; przypadające 20% kosztów (1 000 zł) wykażemy w PIT/B do PIT-36L,
- b) Alfa → 30% kosztów = 1 500 zł,
- c) Beta → 50% kosztów = 2 500 zł.

---

## Powiązanie z importerem

Automat (`IpBoxKpirImporter`) realizuje **krok 1** schematu: wczytuje koszty i przychody z PDF KPiR i wypełnia miesięczne bloki szablonu ewidencji. Kroki 2–9 (podział na projekty, ustalanie udziałów, weryfikacja kosztów bezpośrednich, wskaźnik nexus) pozostają **ręczne** – wymagają decyzji merytorycznych, których nie da się wyprowadzić z samego KPiR.

Mapowanie kolumn wypełnianych przez importer opisuje [`../ARCHITECTURE.md`](../ARCHITECTURE.md) oraz [`URUCHOMIENIE.md`](URUCHOMIENIE.md).
