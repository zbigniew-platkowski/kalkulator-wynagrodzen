using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kalkulator.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace Kalkulator.API.Services;

/// <summary>
/// Serwis odpowiedzialny za generowanie tokenów JWT.
/// Token JWT to zaszyfrowany ciąg znaków który zawiera informacje
/// o zalogowanym użytkowniku (id, login, rola).
/// Frontend przechowuje go i wysyła przy każdym żądaniu do API.
/// </summary>
public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Generuje token JWT dla zalogowanego użytkownika.
    /// Token zawiera "claims" - informacje o użytkowniku zakodowane w tokenie.
    /// </summary>
    public string GenerujToken(Uzytkownik uzytkownik)
    {
        // Klucz do podpisywania tokenu - musi być identyczny jak w appsettings.json
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims - informacje zakodowane w tokenie
        // Frontend może je odczytać żeby wiedzieć kto jest zalogowany
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, uzytkownik.Id.ToString()),
            new Claim(ClaimTypes.Name, uzytkownik.Login),
            new Claim(ClaimTypes.Role, uzytkownik.Rola.ToString()),
            new Claim("rola", uzytkownik.Rola.ToString()) // dodatkowy claim dla frontendu
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}