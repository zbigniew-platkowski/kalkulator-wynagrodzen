using Kalkulator.API.Models;

namespace Kalkulator.API.Services;

/// <summary>
/// Główny silnik obliczeniowy systemu.
/// Implementuje wszystkie wzory matematyczne z rozdziału 3 specyfikacji.
/// 
/// WAŻNE: Wszystkie obliczenia używają typu 'decimal' (nie 'double'!).
/// Typ decimal eliminuje błędy zaokrągleń typowe dla arytmetyki zmiennoprzecinkowej.
/// Jest to wymóg WNF-3.3 specyfikacji.
/// </summary>
public class KalkulatorPlacowy
{
    // ================================================================
    // STAŁE STAWKI - wartości z sekcji 3.1 specyfikacji
    // W docelowej implementacji te wartości powinny być pobierane
    // z tabeli ParametryGlobalne w bazie danych (dla danego roku)
    // ================================================================

    private const decimal KEM = 0.0976m;   // składka emerytalna pracownik
    private const decimal KRE = 0.0150m;   // składka rentowa pracownik
    private const decimal KCH = 0.0245m;   // składka chorobowa pracownik
    private const decimal KZD = 0.0900m;   // składka zdrowotna
    private const decimal PROG_PODATKOWY = 120_000m;
    private const decimal STAWKA_PIT_1 = 0.12m;
    private const decimal STAWKA_PIT_2 = 0.32m;
    private const decimal LIMIT_PIT0 = 85_528m;
    private const decimal LIMIT_KUP50 = 120_000m;
    private const decimal LIMIT_ZFSS = 1_000m;

    // Łączny wskaźnik składek pracownika (używany przy zasiłkach)
    // 9.76% + 1.50% + 2.45% = 13.71%
    private const decimal WSKAZNIK_SKLADEK = 0.1371m;

    // ================================================================
    // GŁÓWNA METODA PUBLICZNA
    // Przyjmuje wszystkie dane wejściowe, zwraca kompletny wynik
    // ================================================================

    /// <summary>
    /// Wylicza kompletny pasek wynagrodzeń dla pracownika.
    /// To jest punkt wejścia - wywołujcie tę metodę z kontrolera HR.
    /// </summary>
    public WynikKalkulacji Oblicz(ParametryObliczen p)
{
    bool maL4 = p.DniChoroby > 0 || p.DniOpieki > 0;
    bool maUrlop = p.DniUrlopuMacierzynskiego > 0 || p.DniUrlopuOjcowskiego > 0;

    if (maL4)
        return ObliczZL4(p);
    if (maUrlop)
        return ObliczZUrlopem(p);

    // Sprawdź czy aktywna ulga PIT-0
    if (p.StatusPIT0 != StatusPitZero.BRAK)
        return ObliczZPit0(p);

    // Standardowy miesiąc
    return ObliczStandardowy(p);
}

    // ================================================================
    // SEKCJA 3.1 - Standardowe wyliczenie (umowa o pracę)
    // ================================================================

    /// <summary>
    /// Algorytm standardowy - pracownik bez absencji i bez specjalnych ulg.
    /// Implementuje wzory 1-10 ze specyfikacji.
    /// </summary>
    private WynikKalkulacji ObliczStandardowy(ParametryObliczen p)
    {
        decimal B = p.WynagrodzenieBrutto + p.Premia + p.Nadgodziny + p.Prowizja;

        // --- Krok 1: Składki społeczne (wzory 1-4) ---
        decimal Sem = B * KEM;                          // wzór (1)
        decimal Sre = B * KRE;                          // wzór (2)
        decimal Sch = B * KCH;                          // wzór (3)
        decimal SZUS = Sem + Sre + Sch;                 // wzór (4)

        // --- Krok 2: Składka zdrowotna (wzory 5-6) ---
        decimal Pzd = B - SZUS;                         // wzór (5)
        decimal Szd = Round2(Pzd * KZD);                // wzór (6)

        // --- Krok 3: Podstawa opodatkowania i podatek ---
        decimal KUP = WyznaczKUP(p, B, SZUS);
        decimal PPit = WyznaczPodstaweOpodatkowania(B, SZUS, KUP, p);
        decimal Spod = WyznaczPodatekBazowy(PPit, p.SkumulowanyDochodRoku);
        decimal ZPit = WyznaczZaliczke(Spod, p.KwotaZmniejszajaca);  // wzór (9)

        // --- Krok 4: PPK ---
        decimal PPKprac = p.CzyPPK ? Round2(B * p.StawkaPPKPracownika) : 0m;
        decimal PPKfirm = p.CzyPPK ? Round2(B * p.StawkaPPKPracodawcy) : 0m;

        // Jeśli PPK aktywne - powiększamy podstawę o wpłatę pracodawcy (wzór 26)
        if (p.CzyPPK)
        {
            PPit = WyznaczPodstaweOpodatkowania(B + PPKfirm, SZUS, KUP, p);
            Spod = WyznaczPodatekBazowy(PPit, p.SkumulowanyDochodRoku);
            ZPit = WyznaczZaliczke(Spod, p.KwotaZmniejszajaca);
        }

        // --- Krok 5: Netto (wzór 10) ---
        decimal N = B - SZUS - Szd - ZPit - PPKprac;

        // --- Krok 6: ZFŚS (wczasy pod gruszą) ---
        decimal Wgrusz = ObliczZFSS(p.SwiadczenieZFSS, p.SkumulowaneZFSSRoku, ref PPit, ref ZPit);
        if (Wgrusz > 0) N += Wgrusz;

        // --- Krok 7: Koszty pracodawcy (Super Brutto) ---
        var kosztPracodawcy = ObliczKosztPracodawcy(B, p.StawkaWypadkowa, PPKfirm);

        return ZbudujWynik(
            baza: B,
            szus: SZUS,
            szd: Szd,
            zpIt: ZPit,
            ppkPrac: PPKprac,
            ppkFirm: PPKfirm,
            netto: N,
            koszt: kosztPracodawcy,
            podstawaOpodatkowania: (int)PPit
        );
    }

    // ================================================================
    // SEKCJA 3.2.1 - Ulga PIT-0
    // ================================================================

    private WynikKalkulacji ObliczZPit0(ParametryObliczen p)
    {
        decimal B = p.WynagrodzenieBrutto + p.Premia + p.Nadgodziny + p.Prowizja;

        // Wyznaczamy ile z bieżącego brutto mieści się w limicie (wzór 11)
        decimal Bzwol = Math.Min(B, Math.Max(0, LIMIT_PIT0 - p.SkumulowanyPrzychodRoku));
        decimal Bopod = B - Bzwol;                      // wzór (12)

        // Składki liczymy od pełnego brutto (wzory 13-14)
        decimal SZUS = Round2(B * (KEM + KRE + KCH));   // wzór (13)
        decimal Szd = Round2((B - SZUS) * KZD);         // wzór (14)

        // Podstawa opodatkowania tylko od części przekraczającej limit (wzór 16)
        decimal SZUSopod = SZUS * (Bopod / B);          // wzór (15)
        decimal KUP = Bopod > 0 ? p.KUPStandard : 0m;
        decimal PPit = Math.Max(0, Round0(Bopod - SZUSopod - KUP)); // wzór (16)

        decimal Spod = WyznaczPodatekBazowy(PPit, p.SkumulowanyDochodRoku);
        decimal ZPit = WyznaczZaliczke(Spod, p.KwotaZmniejszajaca); // wzór (17)

        decimal N = B - SZUS - Szd - ZPit;              // wzór (18)

        var kosztPracodawcy = ObliczKosztPracodawcy(B, p.StawkaWypadkowa, 0m);

        return ZbudujWynik(B, SZUS, Szd, ZPit, 0, 0, N, kosztPracodawcy, (int)PPit);
    }

    // ================================================================
    // SEKCJA 3.2.2 - KUP 50% (honorarium autorskie)
    // ================================================================

    /// <summary>
    /// Wyznacza łączne koszty uzyskania przychodu.
    /// Obsługuje zarówno standardowe KUP jak i 50% dla twórców (wzory 19-23).
    /// </summary>
    private decimal WyznaczKUP(ParametryObliczen p, decimal B, decimal SZUS)
    {
        // Jeśli brak pracy twórczej - zwróć standardowe KUP
        if (p.WspolczynnikAutorski <= 0)
            return p.KUPStandard;

        // Baza dla honorarium autorskiego (wzór 19)
        decimal Paut = (B - SZUS) * p.WspolczynnikAutorski;

        // Teoretyczne KUP 50% (wzór 20)
        decimal KUP50teor = Paut * 0.5m;

        // Ograniczenie do rocznego limitu 120 000 PLN (wzór 21)
        decimal KUP50rzecz = Math.Min(KUP50teor,
            Math.Max(0, LIMIT_KUP50 - p.SkumulowaneKUP50Roku));

        // Standardowe KUP dla pozostałej części (wzór 22)
        decimal KUPstd = p.WspolczynnikAutorski < 1 ? p.KUPStandard : 0m;

        decimal KUPcalk = KUP50rzecz + KUPstd;

        // Zabezpieczenie - KUP nie może przekroczyć podstawy (wzór 22 - uwaga)
        return Math.Min(KUPcalk, B - SZUS);
    }

    // ================================================================
    // SEKCJA 3.2.3 - PPK (obsługiwane w ObliczStandardowy)
    // Wzory 24-27 zaimplementowane powyżej
    // ================================================================

    // ================================================================
    // SEKCJA 3.2.4 - Zwolnienia lekarskie (L4)
    // ================================================================

    /// <summary>
    /// Algorytm dla miesiąca z L4 lub zwolnieniem opiekuńczym.
    /// Implementuje wzory 28-38 ze specyfikacji.
    /// Kluczowa różnica: rozróżnienie wynagrodzenia chorobowego (pracodawca)
    /// od zasiłku chorobowego (ZUS) - wpływa na podstawę składki zdrowotnej.
    /// </summary>
    private WynikKalkulacji ObliczZL4(ParametryObliczen p)
    {
        decimal B = p.WynagrodzenieBrutto;

        // Podstawa wymiaru zasiłku (wzór 28)
        decimal Pzas = Round2(p.SredniaBrutto12 * (1 - WSKAZNIK_SKLADEK));

        // Całkowita liczba dni nieobecności
        int Dsuma = p.DniChoroby + p.DniOpieki;

        // Wynagrodzenie za przepracowane dni (wzór 29)
        decimal Bpraca = Math.Max(0, B - (B / 30m * Dsuma));

        // Podział dni L4 na: finansowane przez pracodawcę vs ZUS (wzory 30-31)
        int Dpracodawca = Math.Min(p.DniChoroby, Math.Max(0, 33 - p.SkumulowaneDniL4Roku));
        int Dzus = p.DniChoroby - Dpracodawca;

        // Kwoty świadczeń (wzory 32-34)
        decimal Wchor = Round2((Pzas / 30m) * Dpracodawca * p.WspolczynnikChorobowy);
        decimal Zchor = Round2((Pzas / 30m) * Dzus * p.WspolczynnikChorobowy);
        decimal Zopieka = Round2((Pzas / 30m) * p.DniOpieki * 0.80m);
        decimal SL4 = Wchor + Zchor + Zopieka;

        // Składki społeczne - tylko od przepracowanej części (wzór 35)
        decimal SZUS = Round2(Bpraca * WSKAZNIK_SKLADEK);

        // Składka zdrowotna - Wchor WCHODZI do podstawy, Zchor i Zopieka NIE (wzór 36)
        decimal Szd = Round2((Bpraca - SZUS + Wchor) * KZD);

        // Podstawa opodatkowania - łączy wynagrodzenie z wszystkimi świadczeniami (wzór 37)
        decimal PPit = Math.Max(0, Round0(Bpraca - SZUS - p.KUPStandard + SL4));

        decimal Spod = WyznaczPodatekBazowy(PPit, p.SkumulowanyDochodRoku);
        decimal ZPit = WyznaczZaliczke(Spod, p.KwotaZmniejszajaca);

        // Netto (wzór 38)
        decimal N = Bpraca - SZUS - Szd - ZPit + SL4;

        // Koszty pracodawcy - podstawa zredukowana do Bpraca (wzory 68-72)
        var kosztPracodawcy = ObliczKosztPracodawcyZL4(
            Bpraca, Wchor, p.StawkaWypadkowa,
            p.CzyPPK ? Round2(Bpraca * p.StawkaPPKPracodawcy) : 0m);

        var wynik = ZbudujWynik(Bpraca, SZUS, Szd, ZPit, 0, 0, N, kosztPracodawcy, (int)PPit);
        wynik.WynagrodzenieChoroboweFirma = Wchor;
        wynik.ZasilkiZus = Zchor + Zopieka;
        return wynik;
    }

    // ================================================================
    // SEKCJA 3.2.5 - Urlop macierzyński i rodzicielski
    // SEKCJA 3.2.6 - Urlop ojcowski
    // ================================================================

    /// <summary>
    /// Algorytm dla miesiąca z urlopem macierzyńskim lub ojcowskim.
    /// Implementuje wzory 41-54 ze specyfikacji.
    /// Zasiłki są zwolnione ze składek ZUS, podlegają tylko PIT.
    /// </summary>
    private WynikKalkulacji ObliczZUrlopem(ParametryObliczen p)
    {
        decimal B = p.WynagrodzenieBrutto;
        int Dmac = p.DniUrlopuMacierzynskiego + p.DniUrlopuOjcowskiego;

        // Podstawa wymiaru zasiłku (wzór 41/48)
        decimal Pzas = Round2(p.SredniaBrutto12 * (1 - WSKAZNIK_SKLADEK));

        // Kwota zasiłku (wzór 42/49)
        decimal Zmac = Round2((Pzas / 30m) * Dmac * p.WspolczynnikZasilku);

        // Wynagrodzenie za przepracowane dni (wzór 43/50)
        decimal Bpraca = Math.Max(0, B - (B / 30m * Dmac));

        // Składki tylko od przepracowanej części (wzory 44-45 / 51-52)
        decimal SZUS = Round2(Bpraca * WSKAZNIK_SKLADEK);
        decimal Szd = Round2((Bpraca - SZUS) * KZD);

        // Podstawa opodatkowania łączy pracę z zasiłkiem (wzór 46/53)
        decimal PPit = Math.Max(0, Round0(Bpraca - SZUS - p.KUPStandard + Zmac));

        decimal Spod = WyznaczPodatekBazowy(PPit, p.SkumulowanyDochodRoku);
        decimal ZPit = WyznaczZaliczke(Spod, p.KwotaZmniejszajaca);

        // Netto (wzór 47/54)
        decimal N = Bpraca - SZUS - Szd - ZPit + Zmac;

        // Koszty pracodawcy - tylko od Bpraca (wzór 73)
        var kosztPracodawcy = ObliczKosztPracodawcy(Bpraca, p.StawkaWypadkowa,
            p.CzyPPK ? Round2(Bpraca * p.StawkaPPKPracodawcy) : 0m);

        var wynik = ZbudujWynik(Bpraca, SZUS, Szd, ZPit, 0, 0, N, kosztPracodawcy, (int)PPit);
        wynik.ZasilkiZus = Zmac;
        return wynik;
    }

    // ================================================================
    // SEKCJA 3.2.7 - Wczasy pod gruszą (ZFŚS)
    // ================================================================

    /// <summary>
    /// Obsługuje dofinansowanie z ZFŚS (wzory 55-58).
    /// Kwota do limitu 1000 PLN rocznie - zwolniona z podatku.
    /// Nadwyżka - doliczana do podstawy opodatkowania.
    /// ZFŚS nigdy nie wchodzi do podstawy składek ZUS.
    /// </summary>
    private decimal ObliczZFSS(decimal Wgrusz, decimal RzfsPop, ref decimal PPit, ref decimal ZPit)
    {
        if (Wgrusz <= 0) return 0m;

        // Część zwolniona z podatku (wzór 55)
        decimal Wzwol = Math.Min(Wgrusz, Math.Max(0, LIMIT_ZFSS - RzfsPop));

        // Część opodatkowana (wzór 56)
        decimal Wopod = Wgrusz - Wzwol;

        // Jeśli jest nadwyżka - powiększa podstawę opodatkowania (wzór 57)
        if (Wopod > 0)
        {
            PPit = Math.Max(0, Round0(PPit + Wopod));
            // Przeliczamy podatek - uproszczone, zakładamy że nie przekraczamy progu
            ZPit = Round0(PPit * STAWKA_PIT_1);
        }

        return Wgrusz; // Cała kwota trafia do wypłaty (wzór 58)
    }

    // ================================================================
    // SEKCJA 3.3 - Koszty pracodawcy (Super Brutto)
    // ================================================================

    /// <summary>
    /// Wylicza całkowity koszt zatrudnienia po stronie pracodawcy.
    /// Implementuje wzory 59-67 ze specyfikacji.
    /// </summary>
    private KosztPracodawcy ObliczKosztPracodawcy(decimal B, decimal kwyp, decimal PPKfirm)
    {
        // Składki emerytalna i rentowa pracodawcy (wzory 59-60)
        decimal SemFirm = Round2(B * 0.0976m);
        decimal SreFirm = Round2(B * 0.0650m);

        // Składka wypadkowa - stawka zmienna (wzór 61)
        decimal SwypFirm = Round2(B * kwyp);

        decimal SZUSfirm = SemFirm + SreFirm + SwypFirm;   // wzór (62)

        // Fundusze pozaubezpieczeniowe (wzory 63-64)
        decimal SFP = Round2(B * 0.0245m);      // FP + FS łącznie
        decimal SFGSP = Round2(B * 0.0010m);    // FGŚP

        decimal ZUSPracodawcy = SZUSfirm + SFP + SFGSP;    // wzór (65)

        // Całkowity koszt (wzór 67)
        decimal Kcalk = B + ZUSPracodawcy + PPKfirm;

        return new KosztPracodawcy
        {
            SkladkaEmerytalna = SemFirm,
            SkladkaRentowa = SreFirm,
            SkladkaWypadkowa = SwypFirm,
            FunduszPracy = SFP,
            FunduszGwarSwiadczen = SFGSP,
            PPK = PPKfirm,
            SuperBrutto = Round2(Kcalk)
        };
    }

    /// <summary>
    /// Wersja dla miesięcy z L4 - podstawa zredukowana do Bpraca (wzory 68-72).
    /// </summary>
    private KosztPracodawcy ObliczKosztPracodawcyZL4(
        decimal Bpraca, decimal Wchor, decimal kwyp, decimal PPKfirm)
    {
        var koszt = ObliczKosztPracodawcy(Bpraca, kwyp, PPKfirm);

        // Do 33. dnia pracodawca wypłaca Wchor z własnych środków (wzór 71)
        koszt.SuperBrutto = Round2(Bpraca + Wchor +
            koszt.SkladkaEmerytalna + koszt.SkladkaRentowa + koszt.SkladkaWypadkowa +
            koszt.FunduszPracy + koszt.FunduszGwarSwiadczen + koszt.PPK);

        return koszt;
    }

    // ================================================================
    // METODY POMOCNICZE
    // ================================================================

    /// <summary>
    /// Wyznacza podatek bazowy uwzględniając próg podatkowy (wzór 8).
    /// Obsługuje miesiąc przekroczenia progu 120 000 PLN.
    /// </summary>
    private decimal WyznaczPodatekBazowy(decimal PPit, decimal Dpop)
    {
        decimal Dakt = Dpop + PPit;

        if (Dakt <= PROG_PODATKOWY)
            // Cały dochód poniżej progu - stawka 12%
            return PPit * STAWKA_PIT_1;

        if (Dpop <= PROG_PODATKOWY && Dakt > PROG_PODATKOWY)
            // Miesiąc przekroczenia progu - część 12%, część 32%
            return (PROG_PODATKOWY - Dpop) * STAWKA_PIT_1
                 + (Dakt - PROG_PODATKOWY) * STAWKA_PIT_2;

        // Cały dochód powyżej progu - stawka 32%
        return PPit * STAWKA_PIT_2;
    }

    /// <summary>
    /// Wyznacza ostateczną zaliczkę po odjęciu kwoty zmniejszającej (wzór 9).
    /// Nigdy nie może być ujemna (edge case z sekcji 10.2).
    /// </summary>
    private decimal WyznaczZaliczke(decimal Spod, decimal uzmn)
        => Math.Max(0, Round0(Spod - uzmn));

    /// <summary>
    /// Wyznacza podstawę opodatkowania (wzór 7 / 23).
    /// Uwzględnia opcjonalne KUP 50% przez metodę WyznaczKUP.
    /// </summary>
    private decimal WyznaczPodstaweOpodatkowania(
        decimal B, decimal SZUS, decimal KUP, ParametryObliczen p)
        => Math.Max(0, Round0(B - SZUS - KUP));

    /// <summary>
    /// Buduje obiekt wynikowy WynikKalkulacji z obliczonych wartości.
    /// </summary>
    private WynikKalkulacji ZbudujWynik(
        decimal baza, decimal szus, decimal szd, decimal zpIt,
        decimal ppkPrac, decimal ppkFirm, decimal netto,
        KosztPracodawcy koszt, int podstawaOpodatkowania)
    {
        // Rozbijamy SZUS na składowe (potrzebne do paska płacowego)
        decimal Sem = Round2(baza * KEM);
        decimal Sre = Round2(baza * KRE);
        decimal Sch = Round2(baza * KCH);

        return new WynikKalkulacji
        {
            BazaBruttoPrzepracowana = Round2(baza),
            PodstawaOpodatkowaniaPit = podstawaOpodatkowania,

            // Potrącenia pracownika
            SkladkaEmerytalnaPracownik = Sem,
            SkladkaRentowaPracownik = Sre,
            SkladkaChorobowaPracownik = Sch,
            SkladkaZdrowotna = Round2(szd),
            ZaliczkaPit = Round2(zpIt),
            PpkPracownik = Round2(ppkPrac),
            WynagrodzenieNetto = Round2(netto),

            // Koszty pracodawcy
            SkladkaEmerytalnaPracodawca = koszt.SkladkaEmerytalna,
            SkladkaRentowaPracodawca = koszt.SkladkaRentowa,
            SkladkaWypadkowaPracodawca = koszt.SkladkaWypadkowa,
            FunduszPracy = koszt.FunduszPracy,
            FunduszGwarSwiadczen = koszt.FunduszGwarSwiadczen,
            PpkPracodawca = koszt.PPK,
            SuperBrutto = koszt.SuperBrutto,

            DataWyliczenia = DateTime.UtcNow
        };
    }

    // Zaokrąglenia zgodne z przepisami (sekcja WNF-3.3)
    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static decimal Round0(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);
}

// ================================================================
// KLASY POMOCNICZE - parametry wejściowe i wyjściowe kalkulatora
// ================================================================

/// <summary>
/// Wszystkie dane wejściowe potrzebne do wyliczenia paska.
/// Kontroler HR wypełnia ten obiekt na podstawie żądania HTTP
/// i danych z bazy (profil podatkowy, historia roku).
/// </summary>
public class ParametryObliczen
{
    // --- Dane podstawowe ---
    public decimal WynagrodzenieBrutto { get; set; }
    public decimal Premia { get; set; } = 0;
    public decimal Nadgodziny { get; set; } = 0;
    public decimal Prowizja { get; set; } = 0;
    public decimal SwiadczenieZFSS { get; set; } = 0;

    // --- Profil podatkowy (z tabeli ProfilePodatkowe) ---
    public decimal KUPStandard { get; set; } = 250m;        // 250 lub 300 PLN
    public decimal KwotaZmniejszajaca { get; set; } = 300m; // PIT-2: 300/150/100/0
    public StatusPitZero StatusPIT0 { get; set; } = StatusPitZero.BRAK;
    public decimal WspolczynnikAutorski { get; set; } = 0m; // waut - KUP 50%

    // --- PPK ---
    public bool CzyPPK { get; set; } = false;
    public decimal StawkaPPKPracownika { get; set; } = 0.02m;
    public decimal StawkaPPKPracodawcy { get; set; } = 0.015m;

    // --- Absencje ---
    public int DniChoroby { get; set; } = 0;                // L4
    public int DniOpieki { get; set; } = 0;                 // zwolnienie opiekuńcze
    public int DniUrlopuMacierzynskiego { get; set; } = 0;
    public int DniUrlopuOjcowskiego { get; set; } = 0;
    public decimal WspolczynnikChorobowy { get; set; } = 0.80m;  // kchor
    public decimal WspolczynnikZasilku { get; set; } = 0.815m;   // kmac

    // --- Historia roku (YTD) - pobierana z bazy przez SUM() ---
    // Sekcja 7.7: wyliczane na bieżąco, nie keszowane!
    public decimal SkumulowanyDochodRoku { get; set; } = 0;      // Dpop
    public decimal SkumulowanyPrzychodRoku { get; set; } = 0;    // Rpop (dla PIT-0)
    public decimal SkumulowaneKUP50Roku { get; set; } = 0;       // KUPskumulowane
    public decimal SkumulowaneZFSSRoku { get; set; } = 0;        // Rzfs_pop
    public int SkumulowaneDniL4Roku { get; set; } = 0;           // Hpop

    // --- Dane firmy ---
    public decimal StawkaWypadkowa { get; set; } = 0.0167m;

    // --- Dane do zasiłków (średnia z 12 miesięcy) ---
    public decimal SredniaBrutto12 { get; set; } = 0;            // B12
}

/// <summary>
/// Pomocniczy kontener na koszty pracodawcy.
/// Używany wewnętrznie przez kalkulator.
/// </summary>
public class KosztPracodawcy
{
    public decimal SkladkaEmerytalna { get; set; }
    public decimal SkladkaRentowa { get; set; }
    public decimal SkladkaWypadkowa { get; set; }
    public decimal FunduszPracy { get; set; }
    public decimal FunduszGwarSwiadczen { get; set; }
    public decimal PPK { get; set; }
    public decimal SuperBrutto { get; set; }
}