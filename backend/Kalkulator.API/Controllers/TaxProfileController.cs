using Kalkulator.API.Data;
using Kalkulator.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kalkulator.API.Controllers;

[ApiController]
[Route("api/hr/tax-profile")]
[Authorize(Roles = "HR")]
public class TaxProfileController : ControllerBase
{
    private readonly AppDbContext _db;

    public TaxProfileController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/hr/tax-profile/{pracownikId}
    /// Pobiera aktualny profil podatkowy pracownika.
    /// </summary>
    [HttpGet("{pracownikId}")]
    public async Task<ActionResult> GetProfile(int pracownikId)
    {
        var profil = await _db.ProfilePodatkowe
            .Include(p => p.Pracownik)
            .FirstOrDefaultAsync(p => p.PracownikId == pracownikId);

        if (profil == null)
            return NotFound(new { message = "Brak profilu podatkowego." });

        return Ok(new
        {
            pracownikId = profil.PracownikId,
            imie = profil.Pracownik.Imie,
            nazwisko = profil.Pracownik.Nazwisko,
            statusPitZero = profil.StatusPitZero.ToString(),
            kupStandardKwota = profil.KupStandardKwota,
            pit2Kwota = profil.Pit2Kwota,
            wspolczynnikAutorskiKup = profil.WspolczynnikAutorskiKup,
            ppkStawkaPracownika = profil.PpkStawkaPracownika,
            ppkStawkaPracodawcy = profil.PpkStawkaPracodawcy,
        });
    }

    /// <summary>
    /// PUT /api/hr/tax-profile/{pracownikId}
    /// Aktualizuje profil podatkowy pracownika - implementuje WF-1.1.
    /// </summary>
    [HttpPut("{pracownikId}")]
    public async Task<ActionResult> UpdateProfile(int pracownikId,
        [FromBody] TaxProfileRequest request)
    {
        var profil = await _db.ProfilePodatkowe
            .FirstOrDefaultAsync(p => p.PracownikId == pracownikId);

        if (profil == null)
            return NotFound(new { message = "Brak profilu podatkowego." });

        // Walidacja stawek PPK
        if (request.PpkStawkaPracownika > 0 &&
            (request.PpkStawkaPracownika < 0.005m || request.PpkStawkaPracownika > 0.04m))
            return BadRequest(new { message = "Stawka PPK pracownika musi być między 0.5% a 4%." });

        if (request.PpkStawkaPracodawcy > 0 &&
            (request.PpkStawkaPracodawcy < 0.015m || request.PpkStawkaPracodawcy > 0.04m))
            return BadRequest(new { message = "Stawka PPK pracodawcy musi być między 1.5% a 4%." });

        if (request.WspolczynnikAutorskiKup < 0 || request.WspolczynnikAutorskiKup > 1)
            return BadRequest(new { message = "Współczynnik autorski musi być między 0 a 1." });

        // Aktualizacja
        if (Enum.TryParse<StatusPitZero>(request.StatusPitZero, out var statusPit0))
            profil.StatusPitZero = statusPit0;

        profil.KupStandardKwota = request.KupStandardKwota;
        profil.Pit2Kwota = request.Pit2Kwota;
        profil.WspolczynnikAutorskiKup = request.WspolczynnikAutorskiKup;
        profil.PpkStawkaPracownika = request.PpkStawkaPracownika;
        profil.PpkStawkaPracodawcy = request.PpkStawkaPracodawcy;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Profil podatkowy został zaktualizowany." });
    }
}

public class TaxProfileRequest
{
    public string StatusPitZero { get; set; } = "BRAK";
    public decimal KupStandardKwota { get; set; } = 250m;
    public decimal Pit2Kwota { get; set; } = 300m;
    public decimal WspolczynnikAutorskiKup { get; set; } = 0m;
    public decimal PpkStawkaPracownika { get; set; } = 0.02m;
    public decimal PpkStawkaPracodawcy { get; set; } = 0.015m;
}