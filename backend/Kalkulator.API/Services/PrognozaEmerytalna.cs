namespace Kalkulator.API.Services;

/// <summary>
/// Serwis obliczający prognozę emerytalną.
/// Implementuje wzory 74-78 ze specyfikacji (sekcja 3.4).
/// </summary>
public class PrognozaEmerytalna
{
    // Tablice GUS - średnie dalsze trwanie życia (sekcja 3.4)
    private const decimal T_KOBIETA = 266.4m;   // miesiące dla kobiet (wiek em. 60 lat)
    private const decimal T_MEZCZYZNA = 220.8m;  // miesiące dla mężczyzn (wiek em. 65 lat)

    // Wiek emerytalny
    private const int WIEK_EM_KOBIETA = 60;
    private const int WIEK_EM_MEZCZYZNA = 65;

    // Minimalne staże do emerytury minimalnej (sekcja 3.4.1)
    private const int STAZ_MIN_KOBIETA = 20;
    private const int STAZ_MIN_MEZCZYZNA = 25;

    // Emerytura minimalna 2026
    private const decimal EMERYTURA_MINIMALNA = 1780.96m;

    /// <summary>
    /// Oblicza prognozę emerytalną dla pracownika.
    /// Zwraca wyniki dla wszystkich 3 metod symulacji.
    /// </summary>
    public WynikPrognozy Oblicz(ParametryPrognozy p)
    {
        // Wyznacz płeć i czas do emerytury
        int wiekEmerytalny = p.Plec == 'K' ? WIEK_EM_KOBIETA : WIEK_EM_MEZCZYZNA;
        decimal T = p.Plec == 'K' ? T_KOBIETA : T_MEZCZYZNA;
        int n = Math.Max(0, wiekEmerytalny - p.WiekObecny);

        // Roczna składka emerytalna = 19.52% rocznego brutto
        // (9.76% pracownik + 9.76% pracodawca)
        decimal Srok = p.RoczneWynagrodzenieBrutto * 0.1952m;

        // -------------------------------------------------------
        // Trzy metody symulacji (wzory 75-77)
        // -------------------------------------------------------

        decimal Kap1 = ObliczMetode1(p.KapitalPoczatkowy, p.StopaWaloryzacji, n);
        decimal Kap2 = ObliczMetode2(p.KapitalPoczatkowy, p.StopaWaloryzacji, n, Srok);
        decimal Kap3 = ObliczMetode3(p.KapitalPoczatkowy, p.StopaWaloryzacji, n, Srok, p.StopaWzrostuWynagrodzen);

        // Uwzględnij przerwę w karierze i wymiar etatu (tryb "Co jeśli?")
        if (p.PrzerwaCourierLata > 0 || p.WymiarEtatu < 1.0m)
        {
            Kap2 = ObliczMetode2CoJesli(p, n, Srok);
            Kap3 = ObliczMetode3CoJesli(p, n, Srok);
        }

        // -------------------------------------------------------
        // Miesięczna emerytura (wzór 74)
        // -------------------------------------------------------
        decimal E1 = Kap1 / T;
        decimal E2 = Kap2 / T;
        decimal E3 = Kap3 / T;

        // -------------------------------------------------------
        // Dyskontowanie o inflację (wzór 78)
        // -------------------------------------------------------
        decimal wspolczynnikDyskonta = (decimal)Math.Pow((double)(1 + p.StopaInflacji), n);
        decimal E1real = E1 / wspolczynnikDyskonta;
        decimal E2real = E2 / wspolczynnikDyskonta;
        decimal E3real = E3 / wspolczynnikDyskonta;

        // -------------------------------------------------------
        // Walidacja stażu (sekcja 3.4.1)
        // -------------------------------------------------------
        int stazMinimalny = p.Plec == 'K' ? STAZ_MIN_KOBIETA : STAZ_MIN_MEZCZYZNA;
        bool maStazDoEmerytury = p.StazPracyLata + n >= stazMinimalny;

        // Sprawdź czy przysługuje dopłata do emerytury minimalnej
        bool E2DoEmerytury = maStazDoEmerytury && E2 < EMERYTURA_MINIMALNA;
        bool E3DoEmerytury = maStazDoEmerytury && E3 < EMERYTURA_MINIMALNA;

        // Generuj dane do wykresu (rok po roku)
        var daneDoWykresu = GenerujDaneWykresu(
            p.KapitalPoczatkowy, p.StopaWaloryzacji,
            p.StopaWzrostuWynagrodzen, p.StopaInflacji,
            n, Srok, p.PrzerwaCourierLata, p.WymiarEtatu);

        return new WynikPrognozy
        {
            LatDoEmerytury = n,
            WiekEmerytalny = wiekEmerytalny,

            // Nominalne emerytury
            Metoda1Nominalna = Math.Round(E1, 2),
            Metoda2Nominalna = Math.Round(E2, 2),
            Metoda3Nominalna = Math.Round(E3, 2),

            // Realne emerytury (w dzisiejszych złotówkach)
            Metoda1Realna = Math.Round(E1real, 2),
            Metoda2Realna = Math.Round(E2real, 2),
            Metoda3Realna = Math.Round(E3real, 2),

            // Walidacja stażu
            MaStazDoEmerytury = maStazDoEmerytury,
            PrzyslugujeEmeryturaMinimalna2 = E2DoEmerytury,
            PrzyslugujeEmeryturaMinimalna3 = E3DoEmerytury,
            EmeryturaMinimalna = EMERYTURA_MINIMALNA,

            // Dane do wykresu
            DaneWykresu = daneDoWykresu
        };
    }

    // -------------------------------------------------------
    // Metoda 1: Brak dalszej pracy (wzór 75)
    // -------------------------------------------------------
    private decimal ObliczMetode1(decimal Kapakt, decimal wzus, int n)
        => Kapakt * (decimal)Math.Pow((double)(1 + wzus), n);

    // -------------------------------------------------------
    // Metoda 2: Stała składka (wzór 76)
    // -------------------------------------------------------
    private decimal ObliczMetode2(decimal Kapakt, decimal wzus, int n, decimal Srok)
    {
        decimal kapital = Kapakt * (decimal)Math.Pow((double)(1 + wzus), n);

        for (int i = 1; i <= n; i++)
        {
            kapital += Srok * (decimal)Math.Pow((double)(1 + wzus), n - i + 1);
        }

        return kapital;
    }

    // -------------------------------------------------------
    // Metoda 3: Rosnąca składka (wzór 77)
    // -------------------------------------------------------
    private decimal ObliczMetode3(decimal Kapakt, decimal wzus, int n, decimal Srok, decimal wwyn)
    {
        decimal kapital = Kapakt * (decimal)Math.Pow((double)(1 + wzus), n);

        for (int i = 1; i <= n; i++)
        {
            decimal skladkaRoku = Srok * (decimal)Math.Pow((double)(1 + wwyn), i - 1);
            kapital += skladkaRoku * (decimal)Math.Pow((double)(1 + wzus), n - i + 1);
        }

        return kapital;
    }

    // -------------------------------------------------------
    // Metoda 2 z uwzględnieniem trybu "Co jeśli?"
    // -------------------------------------------------------
    private decimal ObliczMetode2CoJesli(ParametryPrognozy p, int n, decimal Srok)
    {
        decimal kapital = p.KapitalPoczatkowy * (decimal)Math.Pow((double)(1 + p.StopaWaloryzacji), n);

        for (int i = 1; i <= n; i++)
        {
            // W latach przerwy składka wynosi 0
            bool wPrzerwie = i <= p.PrzerwaCourierLata;
            decimal skladkaRoku = wPrzerwie ? 0 : Srok * p.WymiarEtatu;
            kapital += skladkaRoku * (decimal)Math.Pow((double)(1 + p.StopaWaloryzacji), n - i + 1);
        }

        return kapital;
    }

    // -------------------------------------------------------
    // Metoda 3 z uwzględnieniem trybu "Co jeśli?"
    // -------------------------------------------------------
    private decimal ObliczMetode3CoJesli(ParametryPrognozy p, int n, decimal Srok)
    {
        decimal kapital = p.KapitalPoczatkowy * (decimal)Math.Pow((double)(1 + p.StopaWaloryzacji), n);

        for (int i = 1; i <= n; i++)
        {
            bool wPrzerwie = i <= p.PrzerwaCourierLata;
            decimal skladkaRoku = wPrzerwie ? 0
                : Srok * p.WymiarEtatu * (decimal)Math.Pow((double)(1 + p.StopaWzrostuWynagrodzen), i - 1);
            kapital += skladkaRoku * (decimal)Math.Pow((double)(1 + p.StopaWaloryzacji), n - i + 1);
        }

        return kapital;
    }

    // -------------------------------------------------------
    // Generowanie danych do wykresu (rok po roku)
    // -------------------------------------------------------
    private List<PunktWykresu> GenerujDaneWykresu(
        decimal kapital, decimal wzus, decimal wwyn, decimal winf,
        int n, decimal Srok, int przerwa, decimal etat)
    {
        var dane = new List<PunktWykresu>();
        int rokBazowy = DateTime.Now.Year;

        decimal kap1 = kapital;
        decimal kap2 = kapital;
        decimal kap3 = kapital;

        for (int i = 0; i <= n; i++)
        {
            decimal dyskonto = (decimal)Math.Pow((double)(1 + winf), i);

            dane.Add(new PunktWykresu
            {
                Rok = rokBazowy + i,
                Metoda1Nominalna = Math.Round(kap1, 0),
                Metoda2Nominalna = Math.Round(kap2, 0),
                Metoda3Nominalna = Math.Round(kap3, 0),
                Metoda1Realna = Math.Round(kap1 / dyskonto, 0),
                Metoda2Realna = Math.Round(kap2 / dyskonto, 0),
                Metoda3Realna = Math.Round(kap3 / dyskonto, 0),
            });

            if (i < n)
            {
                bool wPrzerwie = i < przerwa;
                decimal skladka2 = wPrzerwie ? 0 : Srok * etat;
                decimal skladka3 = wPrzerwie ? 0
                    : Srok * etat * (decimal)Math.Pow((double)(1 + wwyn), i);

                kap1 = kap1 * (1 + wzus);
                kap2 = kap2 * (1 + wzus) + skladka2;
                kap3 = kap3 * (1 + wzus) + skladka3;
            }
        }

        return dane;
    }
}

// -------------------------------------------------------
// Klasy pomocnicze
// -------------------------------------------------------

public class ParametryPrognozy
{
    public char Plec { get; set; } = 'M';
    public int WiekObecny { get; set; }
    public int StazPracyLata { get; set; }
    public decimal KapitalPoczatkowy { get; set; } = 0;
    public decimal RoczneWynagrodzenieBrutto { get; set; }

    // Parametry symulacji
    public decimal StopaWaloryzacji { get; set; } = 0.05m;      // 5% rocznie
    public decimal StopaWzrostuWynagrodzen { get; set; } = 0.03m; // 3% rocznie
    public decimal StopaInflacji { get; set; } = 0.025m;          // 2.5% cel NBP

    // Tryb "Co jeśli?"
    public int PrzerwaCourierLata { get; set; } = 0;
    public decimal WymiarEtatu { get; set; } = 1.0m;
}

public class WynikPrognozy
{
    public int LatDoEmerytury { get; set; }
    public int WiekEmerytalny { get; set; }

    public decimal Metoda1Nominalna { get; set; }
    public decimal Metoda2Nominalna { get; set; }
    public decimal Metoda3Nominalna { get; set; }

    public decimal Metoda1Realna { get; set; }
    public decimal Metoda2Realna { get; set; }
    public decimal Metoda3Realna { get; set; }

    public bool MaStazDoEmerytury { get; set; }
    public bool PrzyslugujeEmeryturaMinimalna2 { get; set; }
    public bool PrzyslugujeEmeryturaMinimalna3 { get; set; }
    public decimal EmeryturaMinimalna { get; set; }

    public List<PunktWykresu> DaneWykresu { get; set; } = new();
}

public class PunktWykresu
{
    public int Rok { get; set; }
    public decimal Metoda1Nominalna { get; set; }
    public decimal Metoda2Nominalna { get; set; }
    public decimal Metoda3Nominalna { get; set; }
    public decimal Metoda1Realna { get; set; }
    public decimal Metoda2Realna { get; set; }
    public decimal Metoda3Realna { get; set; }
}