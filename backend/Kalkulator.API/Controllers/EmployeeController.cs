using Kalkulator.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kalkulator.API.Controllers;

[ApiController]
[Route("api/employee")]
[Authorize(Roles = "PRACOWNIK")]  // Tylko pracownik ma dostęp
public class EmployeeController : ControllerBase
{
    private readonly AppDbContext _db;

    public EmployeeController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/employee/salary/{pensjaId}
    /// Szczegółowy pasek płacowy dla pracownika.
    /// Implementuje WF-2.1 i WF-2.2 ze specyfikacji.
    /// System weryfikuje czy pracownik odpytuje o SWÓJ pasek.
    /// </summary>
    [HttpGet("salary/{pensjaId}")]
    public async Task<ActionResult> GetPasek(int pensjaId)
    {
        // Pobierz ID zalogowanego użytkownika z tokenu JWT
        var uzytkownikId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // Znajdź pracownika powiązanego z zalogowanym użytkownikiem
        var pracownik = await _db.Pracownicy
            .FirstOrDefaultAsync(p => p.UzytkownikId == uzytkownikId);

        if (pracownik == null)
            return NotFound(new { message = "Nie znaleziono profilu pracownika." });

        // Pobierz pasek z wynikiem kalkulacji
        var wynik = await _db.WynikiKalkulacji
            .Include(w => w.PensjaMiesieczna)
            .FirstOrDefaultAsync(w => w.PensjaId == pensjaId);

        if (wynik == null)
            return NotFound(new { message = "Pasek nie istnieje." });

        // WAŻNE: Sprawdź czy ten pasek należy do zalogowanego pracownika
        // To jest wymóg bezpieczeństwa - pracownik nie może oglądać cudzych pasków
        if (wynik.PensjaMiesieczna.PracownikId != pracownik.Id)
            return Forbid();

        // Zwróć szczegółowe dane zgodnie z kontraktem API (sekcja 8.2)
        return Ok(new
        {
            id = wynik.PensjaId,
            miesiac = wynik.PensjaMiesieczna.Miesiac,
            rok = wynik.PensjaMiesieczna.Rok,
            daneWejsciowe = new
            {
                wynagrodzenieZasadnicze = wynik.PensjaMiesieczna.WynagrodzenieZasadnicze,
                premia = wynik.PensjaMiesieczna.Premia,
                swiadczeniaZus = wynik.SkladkaEmerytalnaPracownik
                               + wynik.SkladkaRentowaPracownik
                               + wynik.SkladkaChorobowaPracownik,
                wynagrodzenieChoroboweFirma = wynik.WynagrodzenieChoroboweFirma
            },
            potraceniaPracownik = new
            {
                skladkaEmerytalna = wynik.SkladkaEmerytalnaPracownik,
                skladkaRentowa = wynik.SkladkaRentowaPracownik,
                skladkaChorobowa = wynik.SkladkaChorobowaPracownik,
                skladkaZdrowotna = wynik.SkladkaZdrowotna,
                ppkPracownik = wynik.PpkPracownik,
                zaliczkaPit = wynik.ZaliczkaPit
            },
            wynagrodzenieNetto = wynik.WynagrodzenieNetto,
            kosztyPracodawcy = new
            {
                skladkaEmerytalna = wynik.SkladkaEmerytalnaPracodawca,
                skladkaRentowa = wynik.SkladkaRentowaPracodawca,
                skladkaWypadkowa = wynik.SkladkaWypadkowaPracodawca,
                funduszPracy = wynik.FunduszPracy,
                funduszGwarantowanychSwiadczen = wynik.FunduszGwarSwiadczen,
                ppkPracodawca = wynik.PpkPracodawca,
                superBrutto = wynik.SuperBrutto
            }
        });
    }

    /// <summary>
    /// GET /api/employee/salary
    /// Lista wszystkich pasków zalogowanego pracownika.
    /// </summary>
    [HttpGet("salary")]
    public async Task<ActionResult> GetWszystkiePaski()
    {
        var uzytkownikId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var pracownik = await _db.Pracownicy
            .FirstOrDefaultAsync(p => p.UzytkownikId == uzytkownikId);

        if (pracownik == null)
            return NotFound(new { message = "Nie znaleziono profilu pracownika." });

        var paski = await _db.WynikiKalkulacji
            .Include(w => w.PensjaMiesieczna)
            .Where(w => w.PensjaMiesieczna.PracownikId == pracownik.Id)
            .OrderByDescending(w => w.PensjaMiesieczna.Rok)
            .ThenByDescending(w => w.PensjaMiesieczna.Miesiac)
            .Select(w => new
            {
                pensjaId = w.PensjaId,
                miesiac = w.PensjaMiesieczna.Miesiac,
                rok = w.PensjaMiesieczna.Rok,
                brutto = w.PensjaMiesieczna.WynagrodzenieZasadnicze,
                netto = w.WynagrodzenieNetto,
                superBrutto = w.SuperBrutto
            })
            .ToListAsync();

        return Ok(paski);
    }
}