using Kalkulator.API.Data;
using Kalkulator.API.DTOs;
using Kalkulator.API.Models;
using Kalkulator.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kalkulator.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public AuthController(AppDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    /// <summary>
    /// POST /api/auth/login
    /// Logowanie użytkownika - zwraca token JWT.
    /// 
    /// Przykład żądania:
    /// {
    ///   "login": "hr.testowy",
    ///   "haslo": "Test1234!"
    /// }
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]  // Ten endpoint jest dostępny bez tokenu - to logowanie!
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // Znajdź użytkownika w bazie po loginie
        var uzytkownik = await _db.Uzytkownicy
            .FirstOrDefaultAsync(u => u.Login == request.Login && u.CzyAktywny);

        if (uzytkownik == null)
            return Unauthorized(new { message = "Nieprawidłowy login lub hasło." });

        // Sprawdź hasło - porównujemy hash z bazy z hashem podanego hasła
        bool hasloPoprawne = BCrypt.Net.BCrypt.Verify(request.Haslo, uzytkownik.HasloHash);

        if (!hasloPoprawne)
            return Unauthorized(new { message = "Nieprawidłowy login lub hasło." });

        // Zaktualizuj datę ostatniego logowania
        uzytkownik.DataOstatniegoLogowania = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Wygeneruj token JWT
        var token = _jwt.GenerujToken(uzytkownik);

        return Ok(new LoginResponse
        {
            Token = token,
            Login = uzytkownik.Login,
            Rola = uzytkownik.Rola.ToString(),
            WygasaO = DateTime.UtcNow.AddMinutes(60)
        });
    }

    /// <summary>
    /// GET /api/auth/me
    /// Zwraca dane zalogowanego użytkownika na podstawie tokenu.
    /// Przydatne dla frontendu żeby sprawdzić kto jest zalogowany.
    /// </summary>
    [HttpGet("me")]
    [Authorize]  // Wymaga tokenu JWT
    public ActionResult<object> Me()
    {
        // Dane użytkownika są zakodowane w tokenie - nie musimy odpytywać bazy
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var login = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var rola = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new { id, login, rola });
    }
}