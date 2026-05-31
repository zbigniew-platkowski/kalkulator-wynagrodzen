using Kalkulator.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kalkulator.API.Controllers;

[ApiController]
[Route("api/employee/portfolio")]
[Authorize(Roles = "PRACOWNIK")]
public class PortfolioController : ControllerBase
{
    private readonly AppDbContext _db;

    public PortfolioController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/employee/portfolio
    /// Rejestr zasileń portfela emerytalnego - implementuje WF-2.3.
    /// Pokazuje miesięczne składki emerytalne pracownika i pracodawcy.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetPortfolio()
    {
        var uzytkownikId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var pracownik = await _db.Pracownicy
            .FirstOrDefaultAsync(p => p.UzytkownikId == uzytkownikId);

        if (pracownik == null)
            return NotFound();

        var wyniki = await _db.WynikiKalkulacji
            .Include(w => w.PensjaMiesieczna)
            .Where(w => w.PensjaMiesieczna.PracownikId == pracownik.Id)
            .OrderBy(w => w.PensjaMiesieczna.Rok)
            .ThenBy(w => w.PensjaMiesieczna.Miesiac)
            .Select(w => new
            {
                pensjaId = w.PensjaId,
                miesiac = w.PensjaMiesieczna.Miesiac,
                rok = w.PensjaMiesieczna.Rok,
                wynagrodzenieBrutto = w.BazaBruttoPrzepracowana,
                skladkaEmerytalnaPracownik = w.SkladkaEmerytalnaPracownik,
                skladkaEmerytalnaPracodawca = w.SkladkaEmerytalnaPracodawca,
                lacznaSkladkaEmerytalna = w.SkladkaEmerytalnaPracownik + w.SkladkaEmerytalnaPracodawca,
                premia = w.PensjaMiesieczna.Premia,
                nadgodziny = w.PensjaMiesieczna.Nadgodziny,
            })
            .ToListAsync();

        decimal sumaLaczna = wyniki.Sum(w =>
            w.skladkaEmerytalnaPracownik + w.skladkaEmerytalnaPracodawca);

        return Ok(new
        {
            wpisy = wyniki,
            sumaLaczna = sumaLaczna
        });
    }
}