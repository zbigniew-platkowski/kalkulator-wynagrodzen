# Kalkulator Wynagrodzeń i Emerytur

Projekt implementacyjny na podstawie specyfikacji.

## Wymagania wstępne

| Narzędzie | Wersja |
|-----------|--------|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | najnowsza |
| [Git](https://git-scm.com/) | najnowsza |
| [VS Code](https://code.visualstudio.com/) | najnowsza |

## Uruchomienie

1. Uruchom **Docker Desktop** i upewnij się, że działa w tle.

2. Przejdź do katalogu projektu i uruchom kontenery:

```bash
cd .\backend\Kalkulator.API\
docker compose up --build
```

Pierwsze uruchomienie pobierze obrazy Docker (~kilka minut).

3. Otwórz aplikację: [http://localhost:3000/login](http://localhost:3000/login)

### Adresy serwisów

| Serwis | URL |
|--------|-----|
| Frontend | http://localhost:3000 |
| Backend API (Swagger) | http://localhost:5000/swagger |
| Baza danych | localhost:5432 |

## Konta testowe

| Login | Hasło | Panel |
|-------|-------|-------|
| `hr` | `hr123` | Panel HR (wprowadzanie wynagrodzeń) |
| `jan.kowalski` | `pracownik123` | Panel Pracownika (paski płacowe) |
| `admin` | `admin123` | Panel Administratora IT (zarządzanie kontami) |