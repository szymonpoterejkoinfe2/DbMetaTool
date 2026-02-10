# DbMetaTool
DbMetaTool
Aplikacja konsolowa zbudowana w oparciu o **.NET 8.0**, która automatyzuje proces generowania skryptów metadanych z bazy danych **Firebird 5.0**. 
Narzędzie pozwala na szybki eksport struktury bazy do formatów tekstowych, skryptowych lub strukturalnych.

## Konfiguracja

Aby aplikacja mogła poprawnie komunikować się z serwerem Firebird, niezbędne jest zdefiniowanie parametrów dostępowych. 

Wszelkie ustawienia dotyczące połączenia znajdują się w katalogu **Config**. Aby przygotować aplikację do pracy, należy:

1.  Otworzyć plik `ConnectionStringManager.cs` znajdujący się w folderze **Config**.
2.  Poprawnie ustawić wartość pola/właściwości odpowiadającej za **Base Connection String**.

### Czym jest Partial Connection String?
Jest to ciąg połączenia zawierający dane autoryzacyjne oraz parametry serwera, ale **pozbawiony parametru `database=`**. Aplikacja automatycznie dokleja ścieżkę do bazy danych podczas wykonywania operacji.

**Przykład poprawnej konfiguracji:**
```csharp
// W pliku Config/ConnectionStringManager.cs
public static string BaseConnectionString = "User=SYSDBA;Password=masterkey;Server=localhost;Port=3050;Charset=UTF8;";
```
# Komendy wywoławcze

Aplikacja wspiera interfejs linii komend (CLI), co pozwala na łatwą automatyzację procesów (np. w skryptach CI/CD). Poniżej znajdują się główne tryby pracy narzędzia:

### 1. Budowanie nowej bazy (`build-db`)
Służy do fizycznego utworzenia nowego pliku bazy danych `.fdb` i automatycznego uruchomienia skryptów inicjalizujących (domen, tabel, procedur).

```bash
DbMetaTool build-db --db-dir "C:\db\fb5" --scripts-dir "C:\scripts"
```
### 2. Eksport skryptów metadanych (`export-scripts`)
Pobiera aktualną strukturę z działającej bazy danych i generuje pliki wyjściowe w wybranym formacie.

```bash
DbMetaTool export-scripts --connection-string "..." --output-dir "C:\out"
```
### 3. Aktualizacja istniejącej bazy (`update-db`)
Uruchamia mechanizm aktualizacji różnicowej. Porównuje pliki skryptów z aktualnym stanem bazy i aplikuje tylko brakujące elementy.

```bash
DbMetaTool update-db --connection-string "..." --scripts-dir "C:\scripts"
```

# Uwagi techniczne

Podczas korzystania z aplikacji oraz analizy kodu źródłowego należy zwrócić uwagę na następujące aspekty implementacyjne:

### 1. Wzorzec Result (Result Pattern)
Logika biznesowa aplikacji została zaimplementowana z wykorzystaniem **Result Pattern**. Oznacza to, że metody nie rzucają wyjątków w sytuacjach przewidywalnych błędów (np. brak dostępu do pliku, błąd składni SQL). Zamiast tego zwracają obiekt `Result`, który zawiera informację o sukcesie lub szczegółowy opis porażki. Pozwala to na bezpieczne procesowanie wielu zadań bez przerywania działania całej aplikacji.

### 2. Asynchroniczność i wydajność
Aplikacja została w całości napisana w sposób asynchroniczny (`async/await`), co w teorii pozwala na efektywne zarządzanie zasobami systemowymi i operacjami wejścia/wyjścia (I/O).

### 3. Ograniczenia wykonawcze (Blokowanie wątku)
Mimo asynchronicznej implementacji wewnętrznych modułów, metody te **nie są uruchamiane w pełni asynchronicznie** na poziomie wejścia do programu. 
* **Powód**: Zgodnie z założeniami projektowymi, plik `Program.cs` nie został zmodyfikowany pod kątem pełnej obsługi asynchronicznego punktu wejścia (`async Task Main`).
* **Skutek**: Wywołania asynchroniczne są blokowane do wykonania synchronicznego. Przy bardzo wymagających zadaniach (np. eksporcie setek dużych procedur lub budowaniu ogromnych baz danych), aplikacja może sprawiać wrażenie "zamrożonej" lub blokować wątek główny do czasu zakończenia operacji.

## Przykładowe dane testowe

W katalogu głównym aplikacji znajduje się folder **`TEST_SCRIPTS`**, który służy do szybkiej weryfikacji poprawności działania narzędzia. 
Zawiera on zestaw gotowych skryptów SQL, które pozwalają na zbudowanie prostej, ale kompletnej bazy danych.

### Zawartość folderu `TEST_SCRIPTS`:
Skrypty zostały przygotowane tak, aby odzwierciedlać realne zależności w bazie danych (Domeny -> Tabele -> Procedury):

1.  **Domeny**: Definicje typów użytkownika (np. `D_CENA`, `D_STATUS`), które są fundamentem struktury bazy.
2.  **Tabele**: Skrypt tworzący tabele wykorzystującą zdefiniowane wcześniej domeny, co pozwala sprawdzić poprawność mapowania pól.
3.  **Procedury**: Przykładowa logika składowana, która pozwala przetestować eksport kodu źródłowego oraz obsługę parametrów wejściowych i wyjściowych.

### Jak użyć danych testowych?
Możesz wykorzystać te skrypty do przetestowania komendy `build-db` lub jako wzorzec do przygotowania własnych migracji:

```bash
DbMetaTool build-db --db-dir "C:\db\test" --scripts-dir ".\TEST_SCRIPTS"
```
Po uruchomieniu powyższej komendy otrzymasz w pełni funkcjonalną bazę testową, na której możesz następnie wypróbować operacje `export-scripts` oraz `update-db`.
