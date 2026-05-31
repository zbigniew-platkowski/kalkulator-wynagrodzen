// ============================================================
// ENUMY - typy wyliczeniowe z sekcji 7.1 specyfikacji
// ============================================================

namespace Kalkulator.API.Models;

public enum RolaUzytkownika
{
    HR,
    PRACOWNIK,
    ADMIN_IT
}

public enum StatusPitZero
{
    BRAK,
    MLODY_DO_26,
    PRACUJACY_EMERYT,
    RODZINA_4_PLUS,
    POWROT_Z_ZAGRANICY
}

public enum TypAbsencji
{
    CHOROBA_L4,
    OPIEKA,
    MACIERZYNSKI,
    RODZICIELSKI,
    OJCOWSKI
}

// ============================================================
// TABELE GŁÓWNE (sekcja 7.2)
// ============================================================

/// <summary>
/// Tabela: Uzytkownicy (sekcja 7.2.1)
/// Konta wszystkich użytkowników systemu - HR, Pracownicy i AdminIT
/// </summary>
public class Uzytkownik
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string HasloHash { get; set; } = string.Empty;   // NIGDY nie trzymamy hasła jako tekstu!
    public RolaUzytkownika Rola { get; set; }
    public bool CzyAktywny { get; set; } = true;
    public DateTime? DataOstatniegoLogowania { get; set; }
}

/// <summary>
/// Tabela: Pracownicy (sekcja 7.2.2)
/// Rozszerzenie konta dla użytkowników z rolą PRACOWNIK.
/// Relacja 1-1 z Uzytkownik.
/// </summary>
public class Pracownik
{
    public int Id { get; set; }
    public int UzytkownikId { get; set; }
    public Uzytkownik Uzytkownik { get; set; } = null!;
    public int WiekObecny { get; set; } = 0;
    public string Imie { get; set; } = string.Empty;
    public string Nazwisko { get; set; } = string.Empty;
    public char Plec { get; set; }                  // 'M' lub 'K' - determinuje wiek emerytalny
    public int StazPracyLata { get; set; }

    /// <summary>
    /// Kapital zgromadzony w ZUS - wprowadzany ręcznie przez pracownika (WF-2.5).
    /// NIEWIDOCZNY dla HR i AdminIT (Privacy by Design).
    /// </summary>
    public decimal KapitalPoczatkowyZus { get; set; } = 0;

    // Nawigacja EF Core
    public ICollection<PensjaMiesieczna> PensjeMiesieczne { get; set; } = new List<PensjaMiesieczna>();
}

/// <summary>
/// Tabela: ProfilePodatkowe (sekcja 7.2.3)
/// Stałe parametry podatkowe pracownika konfigurowane przez HR.
/// Odczytywane przez silnik przy każdej kalkulacji.
/// </summary>
public class ProfilPodatkowy
{
    public int Id { get; set; }
    public int PracownikId { get; set; }
    public Pracownik Pracownik { get; set; } = null!;

    public StatusPitZero StatusPitZero { get; set; } = StatusPitZero.BRAK;
    public decimal KupStandardKwota { get; set; } = 250.00m;   // 'm' = decimal literal w C#
    public decimal Pit2Kwota { get; set; } = 300.00m;
    public decimal WspolczynnikAutorskiKup { get; set; } = 0.00m;  // waut z specyfikacji
    public decimal PpkStawkaPracownika { get; set; } = 0.020m;
    public decimal PpkStawkaPracodawcy { get; set; } = 0.015m;
}

/// <summary>
/// Tabela: ParametryFirmy (sekcja 7.2.4)
/// Stawka wypadkowa zależy od wielkości firmy i branży (PKD).
/// </summary>
public class ParametryFirmy
{
    public int Id { get; set; }
    public int Rok { get; set; }
    public decimal StawkaWypadkowa { get; set; } = 0.0167m;  // domyślnie 1.67% (małe firmy)
}

// ============================================================
// TABELE TRANSAKCYJNE - dane comiesięczne od HR (sekcja 7.3)
// ============================================================

/// <summary>
/// Tabela: PensjeMiesieczne (sekcja 7.3.1)
/// Główna tabela wejściowa. HR wypełnia ją co miesiąc.
/// Uruchomienie silnika kalkulacyjnego następuje po zatwierdzeniu rekordu.
/// </summary>
public class PensjaMiesieczna
{
    public int Id { get; set; }
    public int PracownikId { get; set; }
    public Pracownik Pracownik { get; set; } = null!;

    public int Miesiac { get; set; }    // 1-12
    public int Rok { get; set; }

    public decimal WynagrodzenieZasadnicze { get; set; }
    public decimal Premia { get; set; } = 0;
    public decimal Nadgodziny { get; set; } = 0;
    public decimal Prowizja { get; set; } = 0;
    public decimal SwiadczenieZfss { get; set; } = 0;   // Wczasy pod gruszą
    public string Status { get; set; } = "ZATWIERDZONE";

    // Nawigacja EF Core
    public ICollection<Absencja> Absencje { get; set; } = new List<Absencja>();
    public WynikKalkulacji? WynikKalkulacji { get; set; }  // null dopóki kalkulator nie zadziała
}

/// <summary>
/// Tabela: Absencje (sekcja 7.3.2)
/// Jeden pracownik może mieć kilka absencji w miesiącu (np. L4 + urlop ojcowski).
/// </summary>
public class Absencja
{
    public int Id { get; set; }
    public int PensjaId { get; set; }
    public PensjaMiesieczna PensjaMiesieczna { get; set; } = null!;

    public TypAbsencji Typ { get; set; }
    public int LiczbaDni { get; set; }

    /// <summary>
    /// Wskaźnik zasiłku: 0.80 dla L4, 1.00 dla ojcowskiego, itd.
    /// Odpowiada zmiennej kchor z sekcji 3.2.4 specyfikacji.
    /// </summary>
    public decimal WspolczynnikZasilku { get; set; } = 0.80m;
}

// ============================================================
// TABELE WYNIKOWE (sekcja 7.4)
// ============================================================

/// <summary>
/// Tabela: WynikiKalkulacji (sekcja 7.4.1)
/// Przechowuje WSZYSTKIE kwoty wyliczone przez silnik.
/// ZASADA NIEZMIENNOŚCI: raz zapisany rekord nie jest nadpisywany!
/// Jeśli HR zrobi błąd → poprawia PensjaMiesieczna i odpala kalkulator od nowa.
/// </summary>
public class WynikKalkulacji
{
    public int Id { get; set; }
    public int PensjaId { get; set; }
    public PensjaMiesieczna PensjaMiesieczna { get; set; } = null!;
    public DateTime DataWyliczenia { get; set; } = DateTime.UtcNow;

    // --- Bazy do obliczeń ---
    public decimal BazaBruttoPrzepracowana { get; set; }    // tylko za dni w pracy
    public decimal WynagrodzenieChoroboweFirma { get; set; } // L4 do 33. dnia
    public decimal ZasilkiZus { get; set; }                  // L4 od 34. dnia, macierzyński itp.
    public int PodstawaOpodatkowaniaPit { get; set; }        // zaokrąglona do pełnych złotych!

    // --- Potrącenia z pensji pracownika ---
    public decimal SkladkaEmerytalnaPracownik { get; set; }  // 9.76%
    public decimal SkladkaRentowaPracownik { get; set; }     // 1.50%
    public decimal SkladkaChorobowaPracownik { get; set; }   // 2.45%
    public decimal SkladkaZdrowotna { get; set; }            // 9.00%
    public decimal ZaliczkaPit { get; set; }                 // podatek dochodowy
    public decimal PpkPracownik { get; set; }                // część pracownicza PPK
    public decimal WynagrodzenieNetto { get; set; }          // KWOTA DO WYPŁATY

    // --- Koszty po stronie pracodawcy ---
    public decimal SkladkaEmerytalnaPracodawca { get; set; } // 9.76%
    public decimal SkladkaRentowaPracodawca { get; set; }    // 6.50%
    public decimal SkladkaWypadkowaPracodawca { get; set; }  // zmienna (0.67%-3.30%)
    public decimal FunduszPracy { get; set; }                // FP + FS = 2.45%
    public decimal FunduszGwarSwiadczen { get; set; }        // FGŚP = 0.10%
    public decimal PpkPracodawca { get; set; }               // część pracodawcy PPK
    public decimal SuperBrutto { get; set; }                 // CAŁKOWITY KOSZT ZATRUDNIENIA
}

// ============================================================
// TABELE KONFIGURACYJNE (sekcja 7.5)
// ============================================================

/// <summary>
/// Tabela: ParametryGlobalne (sekcja 7.5.1)
/// Stawki podatkowe i składkowe na dany rok.
/// Dzięki tej tabeli nie ma "zabetonowanych" wartości w kodzie -
/// zmiana stawki = nowy rekord w bazie, bez rekompilacji!
/// </summary>
public class ParametryGlobalne
{
    public int Id { get; set; }
    public int Rok { get; set; }                         // np. 2026

    // Skala podatkowa PIT
    public decimal ProgPodatkowyPit { get; set; } = 120_000m;
    public decimal StawkaPit1 { get; set; } = 0.12m;    // 12% poniżej progu
    public decimal StawkaPit2 { get; set; } = 0.32m;    // 32% powyżej progu
    public decimal KwotaWolnaOdPodatku { get; set; } = 30_000m;
    public decimal LimitPitZero { get; set; } = 85_528m;

    // Składki ZUS pracownika
    public decimal SkladkaEmerytalnaProcent { get; set; } = 0.0976m;
    public decimal SkladkaRentowaPracProcent { get; set; } = 0.0150m;
    public decimal SkladkaChorobowaProcent { get; set; } = 0.0245m;
    public decimal SkladkaZdrowotnaProcent { get; set; } = 0.0900m;

    // Składki ZUS pracodawcy
    public decimal SkladkaRentowaFirmProcent { get; set; } = 0.0650m;

    // Fundusze
    public decimal FunduszPracyProcent { get; set; } = 0.0245m;   // FP + FS łącznie
    public decimal FgspProcent { get; set; } = 0.0010m;
}
