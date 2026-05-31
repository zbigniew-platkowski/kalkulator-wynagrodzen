using Kalkulator.API.Data;
using Kalkulator.API.DTOs;
using Kalkulator.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kalkulator.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN_IT")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("users")]
    public async Task<ActionResult> GetUsers()
    {
        var users = await _db.Uzytkownicy
            .Select(u => new
            {
                u.Id,
                u.Login,
                Rola = u.Rola.ToString(),
                u.CzyAktywny,
                u.DataOstatniegoLogowania
            })
            .ToListAsync();

        return Ok(new { uzytkownicy = users });
    }

    [HttpPost("users")]
    public async Task<ActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        bool loginIstnieje = await _db.Uzytkownicy
            .AnyAsync(u => u.Login == request.Login);

        if (loginIstnieje)
            return BadRequest(new { message = "Login jest już zajęty." });

        if (!Enum.TryParse<RolaUzytkownika>(request.Rola, out var rola))
            return BadRequest(new { message = "Nieprawidłowa rola. Dozwolone: HR, PRACOWNIK" });

        if (rola == RolaUzytkownika.ADMIN_IT)
            return BadRequest(new { message = "Nie można tworzyć kont Admin IT przez API." });

        var uzytkownik = new Uzytkownik
        {
            Login = request.Login,
            HasloHash = BCrypt.Net.BCrypt.HashPassword(request.Haslo),
            Rola = rola,
            CzyAktywny = true
        };

        _db.Uzytkownicy.Add(uzytkownik);
        await _db.SaveChangesAsync();

        if (rola == RolaUzytkownika.PRACOWNIK)
        {
            var pracownik = new Pracownik
            {
                UzytkownikId = uzytkownik.Id,
                Imie = request.Imie,
                Nazwisko = request.Nazwisko,
                Plec = request.Plec,
                StazPracyLata = 0,          // domyślnie 0 - pracownik uzupełni sam
                KapitalPoczatkowyZus = 0    // domyślnie 0 - pracownik uzupełni sam
            };
            _db.Pracownicy.Add(pracownik);
            await _db.SaveChangesAsync();

            var profil = new ProfilPodatkowy { PracownikId = pracownik.Id };
            _db.ProfilePodatkowe.Add(profil);
            await _db.SaveChangesAsync();
        }

        return Created($"/api/admin/users/{uzytkownik.Id}",
            new { message = "Konto zostało utworzone.", id = uzytkownik.Id });
    }

    [HttpPatch("users/{id}/toggle")]
    public async Task<ActionResult> ToggleUser(int id)
    {
        var uzytkownik = await _db.Uzytkownicy.FindAsync(id);
        if (uzytkownik == null)
            return NotFound(new { message = "Użytkownik nie istnieje." });

        uzytkownik.CzyAktywny = !uzytkownik.CzyAktywny;
        await _db.SaveChangesAsync();

        var status = uzytkownik.CzyAktywny ? "odblokowane" : "zablokowane";
        return Ok(new { message = $"Konto zostało {status}." });
    }

    [HttpDelete("users/{id}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        var uzytkownik = await _db.Uzytkownicy.FindAsync(id);
        if (uzytkownik == null)
            return NotFound(new { message = "Użytkownik nie istnieje." });

        _db.Uzytkownicy.Remove(uzytkownik);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Konto zostało usunięte." });
    }
}