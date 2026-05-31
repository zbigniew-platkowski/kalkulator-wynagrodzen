namespace Kalkulator.API.DTOs;

/// <summary>
/// Dane wysyłane przez HR przy wprowadzaniu miesięcznej pensji.
/// Odpowiada kontraktowi z sekcji 8.1 specyfikacji.
/// </summary>
public class SalaryRequest
{
    public int PracownikId { get; set; }
    public int Miesiac { get; set; }        // 1-12
    public int Rok { get; set; }
    public decimal WynagrodzenieZasadnicze { get; set; }
    public decimal Premia { get; set; } = 0;
    public decimal Nadgodziny { get; set; } = 0;
    public decimal Prowizja { get; set; } = 0;
    public decimal SwiadczenieZfss { get; set; } = 0;
    public List<AbsencjaRequest> Absencje { get; set; } = new();
}

/// <summary>
/// Dane absencji w żądaniu HR.
/// </summary>
public class AbsencjaRequest
{
    public string Typ { get; set; } = string.Empty;  // "CHOROBA_L4", "OPIEKA" itd.
    public int LiczbaDni { get; set; }
    public decimal WspolczynnikZasilku { get; set; } = 0.80m;
}