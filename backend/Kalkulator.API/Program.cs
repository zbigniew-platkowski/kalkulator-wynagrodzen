using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Kalkulator.API.Data;
using Kalkulator.API.Services;
using Kalkulator.API.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey = builder.Configuration["Jwt:Key"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<KalkulatorPlacowy>();
builder.Services.AddScoped<PrognozaEmerytalna>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Kalkulator Wynagrodzen API",
        Version = "v1",
        Description = "Loginy testowe: admin/admin123 | hr/hr123 | jan.kowalski/pracownik123"
    });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Wpisz: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// -------------------------------------------------------
// AUTO-MIGRACJA i SEED przy starcie kontenera
// Bez tego backend crashuje bo baza jest pusta
// -------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Retry loop - czekamy az baza PostgreSQL bedzie gotowa
    for (int i = 0; i < 10; i++)
    {
        try
        {
            db.Database.Migrate();
            Console.WriteLine("Migracje zastosowane pomyslnie.");
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Proba {i + 1}/10 polaczenia z baza: {ex.Message}");
            Thread.Sleep(3000);
        }
    }

    // Seed - dodaj konta testowe jesli baza jest pusta
    if (!db.Uzytkownicy.Any())
    {
        Console.WriteLine("Dodawanie kont testowych...");

        var admin = new Uzytkownik
        {
            Login = "admin",
            HasloHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Rola = RolaUzytkownika.ADMIN_IT,
            CzyAktywny = true
        };
        var hr = new Uzytkownik
        {
            Login = "hr",
            HasloHash = BCrypt.Net.BCrypt.HashPassword("hr123"),
            Rola = RolaUzytkownika.HR,
            CzyAktywny = true
        };
        var pracownikUser = new Uzytkownik
        {
            Login = "jan.kowalski",
            HasloHash = BCrypt.Net.BCrypt.HashPassword("pracownik123"),
            Rola = RolaUzytkownika.PRACOWNIK,
            CzyAktywny = true
        };

        db.Uzytkownicy.AddRange(admin, hr, pracownikUser);
        db.SaveChanges();

        var pracownik = new Pracownik
        {
            UzytkownikId = pracownikUser.Id,
            Imie = "Jan",
            Nazwisko = "Kowalski",
            Plec = 'M',
            StazPracyLata = 5,
            KapitalPoczatkowyZus = 0
        };
        db.Pracownicy.Add(pracownik);
        db.SaveChanges();

        var profil = new ProfilPodatkowy
        {
            PracownikId = pracownik.Id,
            StatusPitZero = StatusPitZero.BRAK,
            KupStandardKwota = 250m,
            Pit2Kwota = 300m,
            WspolczynnikAutorskiKup = 0m,
            PpkStawkaPracownika = 0.02m,
            PpkStawkaPracodawcy = 0.015m
        };
        db.ProfilePodatkowe.Add(profil);

        var parametryFirmy = new ParametryFirmy
        {
            Rok = 2026,
            StawkaWypadkowa = 0.0167m
        };
        db.ParametryFirmy.Add(parametryFirmy);

        var parametryGlobalne = new ParametryGlobalne
        {
            Rok = 2026,
            ProgPodatkowyPit = 120000m,
            StawkaPit1 = 0.12m,
            StawkaPit2 = 0.32m,
            KwotaWolnaOdPodatku = 30000m,
            LimitPitZero = 85528m,
            SkladkaEmerytalnaProcent = 0.0976m,
            SkladkaRentowaPracProcent = 0.015m,
            SkladkaChorobowaProcent = 0.0245m,
            SkladkaZdrowotnaProcent = 0.09m,
            SkladkaRentowaFirmProcent = 0.065m,
            FunduszPracyProcent = 0.0245m,
            FgspProcent = 0.001m
        };
        db.ParametryGlobalne.Add(parametryGlobalne);

        db.SaveChanges();
        Console.WriteLine("Seed zakończony. Konta: admin/admin123 | hr/hr123 | jan.kowalski/pracownik123");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
