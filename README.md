# OrderBookTask

## Budowanie projektu

Z katalogu głównego rozwiązania:

```powershell
dotnet build -c Release
```

## Uruchomienie
```powershell
cd OrderBookTask\bin\Release\net8.0
OrderBookTask.exe
```powershell
Aplikacja domyślnie oczekuje pliku `ticks.raw` w tym samym katalogu co plik wykonywalny `OrderBookTask.exe`. Pliki `ticks.raw`, `ticks_sample.csv` i `ticks_result_sample.csv` są kopiowane do katalogu wynikowego podczas budowania projektu.

### Tryb domyślny - zoptymalizowany
```powershell
OrderBookTask.exe
```
lub
```powershell
OrderBookTask.exe --mode optimized
```

### Wskazanie własnej ścieżki do pliku wejściowego
```powershell
OrderBookTask.exe --input C:\ścieżka\do\ticks.raw
```
Skrócona wersja:
```powershell
OrderBookTask.exe -i C:\ścieżka\do\ticks.raw
```
### Tryb pełny
```powershell
OrderBookTask.exe --mode full
```

Przykład z własną ścieżką wejściową:
```powershell
OrderBookTask.exe --mode full --input C:\ścieżka\do\ticks.raw
```

### Plik wynikowy

Aplikacja generuje plik `ticks_result.csv` w katalogu wykonywalnym programu.

## Dostępne tryby
### Optimized

Obliczane są wyłącznie wymagane pola:

* B0
* A0

Pola opcjonalne:

* BQ0
* BN0
* AQ0
* AN0

pozostają puste. 

Tryb ten reprezentuje najszybszą implementację skoncentrowaną na obowiązkowych wymaganiach zadania.

### Full

Obliczane są wszystkie pola:

* B0
* BQ0
* BN0
* A0
* AQ0
* AN0

Tryb ten prezentuje pełną rekonstrukcję agregatów dla najlepszego poziomu BID i ASK.

## Benchmark

Mierzony zakres czasowy:

`Reset + Process`

Poza pomiarem znajdują się:

* odczyt pliku
* dekodowanie binarne
* walidacja
* zapis CSV
* alokacja struktur wynikowych
* komunikaty konsolowe

Aplikacja wykonuje:

* 1 przebieg rozgrzewający (warmup)
* 5 przebiegów pomiarowych

Raportowane są:

* całkowity czas budowy karnetu (ms)
* całkowity czas budowy karnetu (us)
* us/tick
* ns/tick

## Walidacja

Rozwiązanie wykonuje następujące kontrole:

* Walidację poprawności dekodowania względem ticks_sample.csv
* Walidację wyniku względem ticks_result_sample.csv
* Walidację niezmiennika poprawnego karnetu dla określonego przedziału czasu

Dla `24300006000 <= SourceTime <= 53400000000` sprawdzane jest `B0 < A0` dla każdego zrekonstruowanego stanu karnetu.

## Podsumowanie rozwiązania

Aktywne zlecenia przechowywane są w strukturze indeksowanej przez OrderId. Karnet wykorzystuje gęste tablice poziomów cenowych indeksowane bezpośrednio ceną. Najlepsze poziomy BID i ASK są buforowane i przeliczane wyłącznie wtedy, gdy aktualny najlepszy poziom przestaje istnieć.

Zaimplementowano dwa niezależne procesory:

* optimized (B0/A0)
* full (B0/BQ0/BN0/A0/AQ0/AN0)

aby zachować maksymalnie lekki hot path dla wariantu zoptymalizowanego.

## Zastosowane optymalizacje

* gęste tablice poziomów cenowych zamiast struktur drzewiastych
* cache aktualnego B0/A0
* touched-price clearing podczas resetu i czyszczenia karnetu
* własna, stałopozycyjna mapa haszująca z adresowaniem otwartym (LongStateMap) dla aktywnych zleceń (zastępująca Dictionary)
* kolejność obsługi akcji dopasowana do rozkładu danych
* oddzielne procesory dla trybu optimized i full

## Dalsze możliwe optymalizacje

* pakowanie stanu zlecenia, np. Side i Price w jednym polu liczbowym
* sparse fallback dla przyszłych datasetów z bardzo dużym zakresem cen
* osobne mikrobenchmarki, np. BenchmarkDotNet, dla dokładniejszej analizy hot path

## Środowisko

Testowane na:

* .NET 8
* Release
* x64

Kod jest logicznie podzielony na trzy etapy: `RawTickReader` odpowiada za wczytanie i dekodowanie danych, procesory `OrderBookProcessor` / `FullOrderBookProcessor` za budowę karnetu, a `CsvResultWriter` za zapis wyniku.