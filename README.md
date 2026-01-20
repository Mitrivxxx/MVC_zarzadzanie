# MVC_zarzadzanie
# MVC_zarzadzanie

````````

# PM Suite - uruchomienie w Docker (instrukcja)

Wymagania:
- Docker Desktop (Docker Compose obs?ugiwany)
- (opcjonalnie) .env z ustawieniami DB

Krok 1 — przygotowanie .env
1. Skopiuj plik .env.example do .env:
   cp .env.example .env
2. Zmie? has?o w .env (nie wrzucaj .env do repo).

Krok 2 — (opcjonalne) Dodaj dump bazy
- Je?li masz zrzut bazy (full.sql), umie?? go w ./db/init/full.sql — zostanie wykonany przy pierwszym starcie Postgresa.

Krok 3 — (opcjonalne) W??cz automatyczne migracje
- W Program.cs w projekcie dodaj fragment, który wykona db.Database.Migrate() przy starcie (zamie? YourDbContext na swoj? klas? DbContext).
- Je?eli nie chcesz modyfikowa? kodu — mo?esz uruchomi? migracje r?cznie lokalnie przy pomocy dotnet ef przed uruchomieniem kontenerów.

Krok 4 — uruchomienie
1. Zbuduj i uruchom:
   docker compose up --build
2. Aplikacja dost?pna na: http://localhost:5000

Krok 5 — zatrzymanie i usuwanie
- Zatrzymanie: docker compose down
- Usuni?cie wolumenu danych (usuwa DB): docker compose down -v

Uwagi:
- Je?li Twoja aplikacja u?ywa innej nazwy projektu / DLL ni? MyMvcPostgresApp.dll, zaktualizuj Dockerfile i ewentualnie nazw? csproj.
- Nie wysy?aj plików zawieraj?cych has?a (np. .env). Do??cz .env.example z warto?ciami placeholder
