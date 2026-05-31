namespace Kalkulator.API.DTOs;

/// <summary>
/// Dane wysyłane przez użytkownika przy logowaniu.
/// Frontend wysyła POST /api/auth/login z tym obiektem jako JSON.
/// </summary>
public class LoginRequest
{
    public string Login { get; set; } = string.Empty;
    public string Haslo { get; set; } = string.Empty;
}

/// <summary>
/// Odpowiedź serwera po pomyślnym logowaniu.
/// Frontend zapisuje token i używa go przy każdym kolejnym żądaniu.
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Rola { get; set; } = string.Empty;    // "HR", "PRACOWNIK", "ADMIN_IT"
    public DateTime WygasaO { get; set; }
}

/// <summary>
/// Dane potrzebne do stworzenia nowego konta (używane przez Admin IT).
/// </summary>
public class CreateUserRequest
{
    public string Login { get; set; } = string.Empty;
    public string Haslo { get; set; } = string.Empty;
    public string Rola { get; set; } = string.Empty;   // "HR" lub "PRACOWNIK"
    public string Imie { get; set; } = string.Empty;
    public string Nazwisko { get; set; } = string.Empty;
    public char Plec { get; set; } = 'M';
}