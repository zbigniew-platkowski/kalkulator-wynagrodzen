using Microsoft.EntityFrameworkCore;
using Kalkulator.API.Models;

namespace Kalkulator.API.Data;

/// <summary>
/// Główna klasa dostępu do bazy danych.
/// Każda właściwość DbSet<T> odpowiada jednej tabeli w PostgreSQL.
/// EF Core automatycznie generuje SQL na podstawie tych klas.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // -------------------------------------------------------
    // TABELE - każda DbSet<T> = jedna tabela w bazie
    // -------------------------------------------------------
    public DbSet<Uzytkownik> Uzytkownicy => Set<Uzytkownik>();
    public DbSet<Pracownik> Pracownicy => Set<Pracownik>();
    public DbSet<ProfilPodatkowy> ProfilePodatkowe => Set<ProfilPodatkowy>();
    public DbSet<ParametryFirmy> ParametryFirmy => Set<ParametryFirmy>();
    public DbSet<PensjaMiesieczna> PensjeMiesieczne => Set<PensjaMiesieczna>();
    public DbSet<Absencja> Absencje => Set<Absencja>();
    public DbSet<WynikKalkulacji> WynikiKalkulacji => Set<WynikKalkulacji>();
    public DbSet<ParametryGlobalne> ParametryGlobalne => Set<ParametryGlobalne>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // -------------------------------------------------------
        // ENUMY - PostgreSQL nie zna C# enumów, mapujemy na string
        // -------------------------------------------------------
        modelBuilder.Entity<Uzytkownik>()
            .Property(u => u.Rola)
            .HasConversion<string>();

        modelBuilder.Entity<ProfilPodatkowy>()
            .Property(p => p.StatusPitZero)
            .HasConversion<string>();

        modelBuilder.Entity<Absencja>()
            .Property(a => a.Typ)
            .HasConversion<string>();

        // -------------------------------------------------------
        // INDEKSY - zgodnie z sekcją 7.7 specyfikacji
        // -------------------------------------------------------

        // Najczęstsze zapytanie: "pobierz pasek pracownika za dany miesiąc"
        modelBuilder.Entity<PensjaMiesieczna>()
            .HasIndex(p => new { p.PracownikId, p.Rok, p.Miesiac })
            .HasDatabaseName("IX_PensjeMiesieczne_Pracownik_Rok_Miesiac");

        // Login musi być unikalny
        modelBuilder.Entity<Uzytkownik>()
            .HasIndex(u => u.Login)
            .IsUnique();

        // -------------------------------------------------------
        // RELACJE między tabelami
        // -------------------------------------------------------

        // Uzytkownik 1-1 Pracownik
        modelBuilder.Entity<Pracownik>()
            .HasOne(p => p.Uzytkownik)
            .WithOne()
            .HasForeignKey<Pracownik>(p => p.UzytkownikId);

        // Pracownik 1-1 ProfilPodatkowy
        modelBuilder.Entity<ProfilPodatkowy>()
            .HasOne(pp => pp.Pracownik)
            .WithOne()
            .HasForeignKey<ProfilPodatkowy>(pp => pp.PracownikId);

        // Pracownik 1-N PensjeMiesieczne
        modelBuilder.Entity<PensjaMiesieczna>()
            .HasOne(pm => pm.Pracownik)
            .WithMany(p => p.PensjeMiesieczne)
            .HasForeignKey(pm => pm.PracownikId);

        // PensjaMiesieczna 1-N Absencje
        modelBuilder.Entity<Absencja>()
            .HasOne(a => a.PensjaMiesieczna)
            .WithMany(pm => pm.Absencje)
            .HasForeignKey(a => a.PensjaId);

        // PensjaMiesieczna 1-1 WynikKalkulacji (wynik jest unikalny dla paska)
        modelBuilder.Entity<WynikKalkulacji>()
            .HasOne(w => w.PensjaMiesieczna)
            .WithOne(pm => pm.WynikKalkulacji)
            .HasForeignKey<WynikKalkulacji>(w => w.PensjaId);
    }
}
