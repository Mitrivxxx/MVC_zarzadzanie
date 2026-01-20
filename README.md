# MVC_zarzadzanie
# MVC_zarzadzanie

````````

# PM Suite — uruchomienie w Docker (instrukcja dla recenzenta)

Zawarto?? paczki:
- Kod ?ród?owy aplikacji (Razor Pages / MVC)
- Dockerfile
- docker-compose.yml
- .env.example (przyk?adowe zmienne ?rodowiskowe)
- db/init/full.sql  (opcjonalnie: zrzut bazy SQL)  LUB folder Migrations/
- README.md (ten plik)

Wymagania:
- Docker Desktop (wersja obs?uguj?ca Docker Compose)
- (opcjonalnie) narz?dzia do rozpakowania ZIP

Szybkie uruchomienie:
1. Skopiuj plik .env.example do .env i uzupe?nij warto?ci:
   - POSTGRES_USER
   - POSTGRES_PASSWORD
   - POSTGRES_DB

   Przyk?ad (PowerShell / Bash):
   cp .env.example .env

2. Uruchom kontenery:
   docker compose up --build

3. Aplikacja dost?pna pod:
   http://localhost:5000

Obs?uga bazy danych:
- Je?li w paczce jest db/init/full.sql: Postgres wykona skrypt przy pierwszym starcie serwisu DB (tworzenie wolumenu).
- Je?li zamiast dumpa dostarczono folder Migrations: aplikacja ma w Program.cs automatyczne db.Database.Migrate() i migracje zostan? zastosowane przy starcie.

Jak wygenerowa? dump (je?li potrzebujesz zrobi? zrzut lokalnej bazy):
- Je?eli baza uruchomiona w kontenerze:
  docker compose exec db pg_dump -U ${POSTGRES_USER} -F p -f /tmp/full.sql ${POSTGRES_DB}
  docker cp $(docker compose ps -q db):/tmp/full.sql ./db/init/full.sql

Bezpiecze?stwo i uwagi:
- Nie do??czaj .env ani plików z has?ami do repo.
- Je?li .env zosta? ju? skomitowany: git rm --cached .env && git commit -m "Remove .env"
- W .env.example u?yj bezpiecznych placeholderów.

Jak przygotowa? ZIP do wys?ania:
- Upewnij si?, ?e .env nie jest w repo i ?e db/init/full.sql lub Migrations s? do??czone.
- U?yj:
  git archive --format zip -o ../MyMvcPostgresApp_source.zip HEAD
  zip -g ../MyMvcPostgresApp_source.zip db/init/full.sql README.md .env.example

Kontakt:
- Je?li co? nie dzia?a — sprawd? logi:
  docker compose logs -f web
  docker compose logs -f db

Powodzenia — w razie potrzeby dopracuj? README pod Twoje ?rodowisko.
