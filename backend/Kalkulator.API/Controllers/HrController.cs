using Kalkulator.API.Data;
using Kalkulator.API.DTOs;
using Kalkulator.API.Models;
using Kalkulator.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kalkulator.API.Controllers;

[ApiController]
[Route("api/hr")]
[Authorize(Roles = "HR")]  // Tylko HR ma dostęp
public class HrController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly KalkulatorPlacowy _kalkulator;

    public HrController(AppDbContext db, KalkulatorPlacowy kalkulator)
    {
        _db = db;
        _kalkulator = kalkulator;
    }

    /// <summary>
    /// POST /api/hr/salary
    /// Wprowadzenie danych płacowych przez HR i uruchomienie kalkulatora.
    /// Implementuje WF-1.2, WF-1.3, WF-1.4 ze specyfikacji.
    /// </summary>
    [HttpPost("salary")]
    public async Task<ActionResult> DodajPensje([FromBody] SalaryRequest request)
    {
        // Sprawdź czy pracownik istnieje
        var pracownik = await _db.Pracownicy
            .Include(p => p.PensjeMiesieczne)
            .FirstOrDefaultAsync(p => p.Id == request.PracownikId);

        if (pracownik == null)
            return NotFound(new { message = "Pracownik nie istnieje." });

        // Sprawdź czy nie ma już paska za ten miesiąc
        bool juzIstnieje = await _db.PensjeMiesieczne
            .AnyAsync(p => p.PracownikId == request.PracownikId
                        && p.Miesiac == request.Miesiac
                        && p.Rok == request.Rok);

        if (juzIstnieje)
            return BadRequest(new { message = "Pasek za ten miesiąc już istnieje." });

        // Pobierz profil podatkowy pracownika
        var profil = await _db.ProfilePodatkowe
            .FirstOrDefaultAsync(p => p.PracownikId == request.PracownikId);

        if (profil == null)
            return BadRequest(new { message = "Brak profilu podatkowego pracownika." });

        // Pobierz parametry firmy na dany rok
        var parametryFirmy = await _db.ParametryFirmy
            .FirstOrDefaultAsync(p => p.Rok == request.Rok)
            ?? new ParametryFirmy { StawkaWypadkowa = 0.0167m };

        // Pobierz parametry globalne (stawki podatkowe)
        var parametryGlobalne = await _db.ParametryGlobalne
            .FirstOrDefaultAsync(p => p.Rok == request.Rok)
            ?? new ParametryGlobalne { Rok = request.Rok };

        // -------------------------------------------------------
        // Wylicz wartości narastające YTD (sekcja 7.7 specyfikacji)
        // Nie keszujemy - zawsze liczymy przez SUM() z bazy
        // -------------------------------------------------------
        var wynikiRoku = await _db.WynikiKalkulacji
            .Include(w => w.PensjaMiesieczna)
            .Where(w => w.PensjaMiesieczna.PracownikId == request.PracownikId
                     && w.PensjaMiesieczna.Rok == request.Rok)
            .ToListAsync();

        decimal skumulowanyDochod = wynikiRoku.Sum(w => w.PodstawaOpodatkowaniaPit);
        decimal skumulowanyPrzychod = wynikiRoku
            .Sum(w => w.BazaBruttoPrzepracowana + w.WynagrodzenieChoroboweFirma + w.ZasilkiZus);
        decimal skumulowaneKUP50 = 0; // uproszczone
        decimal skumulowaneZFSS = await _db.PensjeMiesieczne
            .Where(p => p.PracownikId == request.PracownikId && p.Rok == request.Rok)
            .SumAsync(p => p.SwiadczenieZfss);

        int skumulowaneDniL4 = await _db.Absencje
            .Include(a => a.PensjaMiesieczna)
            .Where(a => a.PensjaMiesieczna.PracownikId == request.PracownikId
                     && a.PensjaMiesieczna.Rok == request.Rok
                     && a.Typ == TypAbsencji.CHOROBA_L4)
            .SumAsync(a => a.LiczbaDni);

        // Średnia z 12 miesięcy dla zasiłków
        decimal sredniaBrutto12 = wynikiRoku.Count > 0
            ? wynikiRoku.Average(w => w.BazaBruttoPrzepracowana)
            : request.WynagrodzenieZasadnicze;

        // -------------------------------------------------------
        // Zapisz dane wejściowe HR do bazy
        // -------------------------------------------------------
        var pensja = new PensjaMiesieczna
        {
            PracownikId = request.PracownikId,
            Miesiac = request.Miesiac,
            Rok = request.Rok,
            WynagrodzenieZasadnicze = request.WynagrodzenieZasadnicze,
            Premia = request.Premia,
            Nadgodziny = request.Nadgodziny,
            Prowizja = request.Prowizja,
            SwiadczenieZfss = request.SwiadczenieZfss,
            Status = "ZATWIERDZONE"
        };

        // Dodaj absencje
        foreach (var abs in request.Absencje)
        {
            pensja.Absencje.Add(new Absencja
            {
                Typ = Enum.Parse<TypAbsencji>(abs.Typ),
                LiczbaDni = abs.LiczbaDni,
                WspolczynnikZasilku = abs.WspolczynnikZasilku
            });
        }

        _db.PensjeMiesieczne.Add(pensja);
        await _db.SaveChangesAsync();

        // -------------------------------------------------------
        // Uruchom silnik obliczeniowy
        // -------------------------------------------------------
        var absencjeL4 = request.Absencje.Where(a => a.Typ == "CHOROBA_L4").ToList();
        var absencjeOpieka = request.Absencje.Where(a => a.Typ == "OPIEKA").ToList();
        var absencjeMac = request.Absencje.Where(a =>
            a.Typ == "MACIERZYNSKI" || a.Typ == "RODZICIELSKI").ToList();
        var absencjeOjc = request.Absencje.Where(a => a.Typ == "OJCOWSKI").ToList();

        var parametryObliczen = new ParametryObliczen
        {
            WynagrodzenieBrutto = request.WynagrodzenieZasadnicze,
            Premia = request.Premia,
            Nadgodziny = request.Nadgodziny,
            Prowizja = request.Prowizja,
            SwiadczenieZFSS = request.SwiadczenieZfss,

            // Profil podatkowy
            KUPStandard = profil.KupStandardKwota,
            KwotaZmniejszajaca = profil.Pit2Kwota,
            StatusPIT0 = profil.StatusPitZero,
            WspolczynnikAutorski = profil.WspolczynnikAutorskiKup,
            CzyPPK = profil.PpkStawkaPracownika > 0,
            StawkaPPKPracownika = profil.PpkStawkaPracownika,
            StawkaPPKPracodawcy = profil.PpkStawkaPracodawcy,

            // Absencje
            DniChoroby = absencjeL4.Sum(a => a.LiczbaDni),
            DniOpieki = absencjeOpieka.Sum(a => a.LiczbaDni),
            DniUrlopuMacierzynskiego = absencjeMac.Sum(a => a.LiczbaDni),
            DniUrlopuOjcowskiego = absencjeOjc.Sum(a => a.LiczbaDni),
            WspolczynnikChorobowy = absencjeL4.FirstOrDefault()?.WspolczynnikZasilku ?? 0.80m,
            WspolczynnikZasilku = absencjeMac.FirstOrDefault()?.WspolczynnikZasilku ?? 0.815m,

            // Dane firmy
            StawkaWypadkowa = parametryFirmy.StawkaWypadkowa,

            // YTD
            SkumulowanyDochodRoku = skumulowanyDochod,
            SkumulowanyPrzychodRoku = skumulowanyPrzychod,
            SkumulowaneKUP50Roku = skumulowaneKUP50,
            SkumulowaneZFSSRoku = skumulowaneZFSS,
            SkumulowaneDniL4Roku = skumulowaneDniL4,
            SredniaBrutto12 = sredniaBrutto12
        };

        var wynik = _kalkulator.Oblicz(parametryObliczen);
        wynik.PensjaId = pensja.Id;

        // -------------------------------------------------------
        // Zapisz wynik (zasada niezmienności - sekcja 7.7)
        // -------------------------------------------------------
        _db.WynikiKalkulacji.Add(wynik);
        await _db.SaveChangesAsync();

        return Created($"/api/employee/salary/{pensja.Id}", new
        {
            status = "Success",
            pensjaId = pensja.Id,
            message = "Dane zostały zapisane, a pensja przeliczona pomyślnie."
        });
    }

    /// <summary>
    /// GET /api/hr/pracownicy
    /// Lista pracowników - żeby HR wiedział jakie ID wybrać w formularzu.
    /// </summary>
    [HttpGet("pracownicy")]
    public async Task<ActionResult> GetPracownicy()
    {
        var pracownicy = await _db.Pracownicy
            .Select(p => new
            {
                p.Id,
                p.Imie,
                p.Nazwisko,
                p.Plec
            })
            .ToListAsync();

        return Ok(pracownicy);
    }
}