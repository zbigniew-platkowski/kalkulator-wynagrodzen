# Struktura projektu Backend

```
Kalkulator.API/
│
├── Controllers/          # Endpointy API (wejście żądań HTTP)
│   ├── AuthController.cs       # POST /api/auth/login
│   ├── HrController.cs         # POST /api/hr/salary  [rola: HR]
│   ├── EmployeeController.cs   # GET  /api/employee/salary/{id}  [rola: PRACOWNIK]
│   └── AdminController.cs      # GET  /api/admin/users  [rola: ADMIN_IT]
│
├── Models/               # Klasy C# odpowiadające tabelom w bazie
│   ├── Uzytkownik.cs
│   ├── Pracownik.cs
│   ├── ProfilPodatkowy.cs
│   ├── PensjaM iesieczna.cs
│   ├── Absencja.cs
│   ├── WynikKalkulacji.cs
│   └── ParametryGlobalne.cs
│
├── DTOs/                 # Obiekty transferu danych (Request/Response JSON)
│   ├── LoginRequest.cs
│   ├── SalaryRequest.cs        # Dane wejściowe od HR
│   └── SalaryResponse.cs       # Pasek płacowy dla pracownika
│
├── Services/             # Logika biznesowa
│   ├── KalkulatorPlacowy.cs    # ← SERCE SYSTEMU - wszystkie wzory z spec.
│   ├── PrognozaEmerytalna.cs   # Symulacje emerytalne (wzory 75-78)
│   └── JwtService.cs           # Generowanie tokenów JWT
│
├── Data/                 # Warstwa dostępu do bazy danych
│   └── AppDbContext.cs         # Konfiguracja EF Core + mapowanie tabel
│
├── Migrations/           # Auto-generowane przez EF Core - NIE EDYTOWAĆ ręcznie
│
├── appsettings.json      # Konfiguracja (connection string, JWT)
└── Program.cs            # Punkt wejścia - rejestracja serwisów
```

## Jak uruchomić projekt lokalnie (bez Dockera)

1. Zainstaluj .NET 8 SDK: https://dotnet.microsoft.com/download
2. Zainstaluj PostgreSQL lokalnie LUB użyj Dockera tylko dla bazy:
   ```
   docker run -e POSTGRES_PASSWORD=KalkulatorPass123! -e POSTGRES_USER=kalkulator_user -e POSTGRES_DB=kalkulator_db -p 5432:5432 postgres:16
   ```
3. W katalogu `Kalkulator.API/`:
   ```
   dotnet restore          # pobierz zależności z NuGet
   dotnet ef migrations add InitialCreate   # stwórz migrację
   dotnet ef database update                # zastosuj migrację (stwórz tabele)
   dotnet run              # uruchom API na localhost:5000
   ```
4. Otwórz Swagger UI: http://localhost:5000/swagger

## Jak uruchomić przez Docker (wszystko naraz)

W katalogu głównym projektu (gdzie jest docker-compose.yml):
```
docker compose up --build
```

Zatrzymanie:
```
docker compose down
```

Zatrzymanie + usunięcie danych z bazy:
```
docker compose down -v
```
