using Kalkulator.API.Data;
using Kalkulator.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kalkulator.API.Controllers;

[ApiController]
[Route("api/employee/retirement")]
[Authorize(Roles = "PRACOWNIK")]
public class RetirementController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PrognozaEmerytalna _prognoza;

    public RetirementController(AppDbContext db, PrognozaEmerytalna prognoza)
    {
        _db = db;
        _prognoza = prognoza;
    }

    /// <summary>
    /// GET /api/employee/retirement/profile
    /// Pobiera zapisany wiek, staż i kapitał ZUS pracownika.
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult> GetProfile()
    {
        var uzytkownikId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var pracownik = await _db.Pracownicy
            .FirstOrDefaultAsync(p => p.UzytkownikId == uzytkownikId);

        if (pracownik == null)
            return NotFound();

        return Ok(new
        {
            wiekObecny = pracownik.WiekObecny,
            stazPracyLata = pracownik.StazPracyLata,
            kapitalZus = pracownik.KapitalPoczatkowyZus,
            plec = pracownik.Plec.ToString()
        });
    }

    /// <summary>
    /// PUT /api/employee/retirement/profile
    /// Pracownik zapisuje wiek, staż pracy i kapitał ZUS.
    /// Privacy by Design - niewidoczne dla HR i Admin IT.
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult> ZapiszProfil([FromBody] ProfilEmerytalnyRequest request)
    {
        var uzytkownikId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var pracownik = await _db.Pracownicy
            .FirstOrDefaultAsync(p => p.UzytkownikId == uzytkownikId);

        if (pracownik == null)
            return NotFound();

        pracownik.WiekObecny = request.WiekObecny;
        pracownik.StazPracyLata = request.StazPracyLata;
        pracownik.KapitalPoczatkowyZus = request.KapitalZus;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Dane emerytalne zostały zapisane." });
    }

    /// <summary>
    /// POST /api/employee/retirement/calculate
    /// Oblicza prognozę emerytalną.
    /// </summary>
    [HttpPost("calculate")]
    public async Task<ActionResult> ObliczPrognozę([FromBody] RetirementRequest request)
    {
        var uzytkownikId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var pracownik = await _db.Pracownicy
            .FirstOrDefaultAsync(p => p.UzytkownikId == uzytkownikId);

        if (pracownik == null)
            return NotFound(new { message = "Nie znaleziono profilu pracownika." });

        decimal srednieMiesieczne = request.MiesieczneBrutto > 0
            ? request.MiesieczneBrutto
            : await _db.WynikiKalkulacji
                .Include(w => w.PensjaMiesieczna)
                .Where(w => w.PensjaMiesieczna.PracownikId == pracownik.Id)
                .AverageAsync(w => (decimal?)w.BazaBruttoPrzepracowana) ?? 5000m;

        var parametry = new ParametryPrognozy
        {
            Plec = pracownik.Plec,
            WiekObecny = request.WiekObecny,
            StazPracyLata = request.StazPracyLata,
            KapitalPoczatkowy = request.KapitalZUS,
            RoczneWynagrodzenieBrutto = srednieMiesieczne * 12,
            StopaWaloryzacji = request.StopaWaloryzacji,
            StopaWzrostuWynagrodzen = request.StopaWzrostuWynagrodzen,
            StopaInflacji = 0.025m,
            PrzerwaCourierLata = request.PrzerwaCourierLata,
            WymiarEtatu = request.WymiarEtatu,
        };

        var wynik = _prognoza.Oblicz(parametry);
        return Ok(wynik);
    }
}

public class ProfilEmerytalnyRequest
{
    public int WiekObecny { get; set; }
    public int StazPracyLata { get; set; }
    public decimal KapitalZus { get; set; }
}

public class RetirementRequest
{
    public int WiekObecny { get; set; }
    public int StazPracyLata { get; set; } = 0;
    public decimal MiesieczneBrutto { get; set; }
    public decimal KapitalZUS { get; set; } = 0;
    public decimal StopaWaloryzacji { get; set; } = 0.05m;
    public decimal StopaWzrostuWynagrodzen { get; set; } = 0.03m;
    public int PrzerwaCourierLata { get; set; } = 0;
    public decimal WymiarEtatu { get; set; } = 1.0m;
}

public class CapitalRequest
{
    public decimal KapitalZUS { get; set; }
}