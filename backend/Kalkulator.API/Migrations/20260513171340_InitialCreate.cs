using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kalkulator.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParametryFirmy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Rok = table.Column<int>(type: "integer", nullable: false),
                    StawkaWypadkowa = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametryFirmy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParametryGlobalne",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Rok = table.Column<int>(type: "integer", nullable: false),
                    ProgPodatkowyPit = table.Column<decimal>(type: "numeric", nullable: false),
                    StawkaPit1 = table.Column<decimal>(type: "numeric", nullable: false),
                    StawkaPit2 = table.Column<decimal>(type: "numeric", nullable: false),
                    KwotaWolnaOdPodatku = table.Column<decimal>(type: "numeric", nullable: false),
                    LimitPitZero = table.Column<decimal>(type: "numeric", nullable: false),
                    SkladkaEmerytalnaProcent = table.Column<decimal>(type: "numeric", nullable: false),
                    SkladkaRentowaPracProcent = table.Column<decimal>(type: "numeric", nullable: false),
                    SkladkaChorobowaProcent = table.Column<decimal>(type: "numeric", nullable: false),
                    SkladkaZdrowotnaProcent = table.Column<decimal>(type: "numeric", nullable: false),
                    SkladkaRentowaFirmProcent = table.Column<decimal>(type: "numeric", nullable: false),
                    FunduszPracyProcent = table.Column<decimal>(type: "numeric", nullable: false),
                    FgspProcent = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametryGlobalne", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Uzytkownicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Login = table.Column<string>(type: "text", nullable: false),
                    HasloHash = table.Column<string>(type: "text", nullable: false),
                    Rola = table.Column<string>(type: "text", nullable: false),
                    CzyAktywny = table.Column<bool>(type: "boolean", nullable: false),
                    DataOstatniegoLogowania = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uzytkownicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pracownicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UzytkownikId = table.Column<int>(type: "integer", nullable: false),
                    Imie = table.Column<string>(type: "text", nullable: false),
                    Nazwisko = table.Column<string>(type: "text", nullable: false),
                    Plec = table.Column<char>(type: "character(1)", nullable: false),
                    StazPracyLata = table.Column<int>(type: "integer", nullable: false),
                    KapitalPoczatkowyZus = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pracownicy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pracownicy_Uzytkownicy_UzytkownikId",
                        column: x => x.UzytkownikId,
                        principalTable: "Uzytkownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PensjeMiesieczne",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PracownikId = table.Column<int>(type: "integer", nullable: false),
                    Miesiac = table.Column<int>(type: "integer", nullable: false),
                    Rok = table.Column<int>(type: "integer", nullable: false),
                    WynagrodzenieZasadnicze = table.Column<decimal>(type: "numeric", nullable: false),
                    Premia = table.Column<decimal>(type: "numeric", nullable: false),
                    Nadgodziny = table.Column<decimal>(type: "numeric", nullable: false),
                    Prowizja = table.Column<decimal>(type: "numeric", nullable: false),
                    SwiadczenieZfss = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PensjeMiesieczne", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PensjeMiesieczne_Pracownicy_PracownikId",
                        column: x => x.PracownikId,
                        principalTable: "Pracownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfilePodatkowe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PracownikId = table.Column<int>(type: "integer", nullable: false),
                    StatusPitZero = table.Column<string>(type: "text", nullable: false),
                    KupStandardKwota = table.Column<decimal>(type: "numeric", nullable: false),
                    Pit2Kwota = table.Column<decimal>(type: "numeric", nullable: false),
                    WspolczynnikAutorskiKup = table.Column<decimal>(type: "numeric", nullable: false),
                    PpkStawkaPracownika = table.Column<decimal>(type: "numeric", nullable: false),
                    PpkStawkaPracodawcy = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfilePodatkowe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfilePodatkowe_Pracownicy_PracownikId",
                        column: x => x.PracownikId,
                        principalTable: "Pracownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Absencje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PensjaId = table.Column<int>(type: "integer", nullable: false),
                    Typ = table.Column<string>(type: "text", nullable: false),
                    LiczbaDni = table.Column<int>(type: "integer", nullable: false),
                    WspolczynnikZasilku = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Absencje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Absencje_PensjeMiesieczne_PensjaId",
                        column: x => x.PensjaId,
                        principalTable: "PensjeMiesieczne",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WynikiKalkulacji",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PensjaId = table.Column<int>(type: "integer", nullable: false),
                    DataWyliczenia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BazaBruttoPrzepracowana = table.Column<decimal>(type: "numeric", nullable: false),
                    WynagrodzenieChoroboweFirma = table.Column<decimal>(type: "numeric", nullable: false),
                    ZasilkiZus = table.Column<decimal>(type: "numeric", nullable: false),
                    PodstawaOpodatkowaniaPit = table.Column<int>(type: "integer", nullable: false),
                    SkladkaEmerytalnaPracownik = table.Column<decimal>(type: "numeric", nullable: false),
                    SkladkaRentowaPracownik = table.Column<decimal>(type: "numeric", nullable: false),
                    SkladkaChorobowaPracownik = table.Column<decimal>(type: "numeric", nullable: false),
                    SkladkaZdrowotna = table.Column<decimal>(type: "numeric", nullable: false),
                    ZaliczkaPit = table.Column<decimal>(type: "numeric", nullable: false),
                    PpkPracownik = table.Column<decimal>(type: "numeric", nullable: false),
                    WynagrodzenieNetto = table.Column<decimal>(type: "numeric", nullable: false),
                    SkladkaEmerytalnaPracodawca = table.Column<decimal>(type: "numeric", nullable: false),
                    SkladkaRentowaPracodawca = table.Column<decimal>(type: "numeric", nullable: false),
                    SkladkaWypadkowaPracodawca = table.Column<decimal>(type: "numeric", nullable: false),
                    FunduszPracy = table.Column<decimal>(type: "numeric", nullable: false),
                    FunduszGwarSwiadczen = table.Column<decimal>(type: "numeric", nullable: false),
                    PpkPracodawca = table.Column<decimal>(type: "numeric", nullable: false),
                    SuperBrutto = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WynikiKalkulacji", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WynikiKalkulacji_PensjeMiesieczne_PensjaId",
                        column: x => x.PensjaId,
                        principalTable: "PensjeMiesieczne",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Absencje_PensjaId",
                table: "Absencje",
                column: "PensjaId");

            migrationBuilder.CreateIndex(
                name: "IX_PensjeMiesieczne_Pracownik_Rok_Miesiac",
                table: "PensjeMiesieczne",
                columns: new[] { "PracownikId", "Rok", "Miesiac" });

            migrationBuilder.CreateIndex(
                name: "IX_Pracownicy_UzytkownikId",
                table: "Pracownicy",
                column: "UzytkownikId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfilePodatkowe_PracownikId",
                table: "ProfilePodatkowe",
                column: "PracownikId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Uzytkownicy_Login",
                table: "Uzytkownicy",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WynikiKalkulacji_PensjaId",
                table: "WynikiKalkulacji",
                column: "PensjaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Absencje");

            migrationBuilder.DropTable(
                name: "ParametryFirmy");

            migrationBuilder.DropTable(
                name: "ParametryGlobalne");

            migrationBuilder.DropTable(
                name: "ProfilePodatkowe");

            migrationBuilder.DropTable(
                name: "WynikiKalkulacji");

            migrationBuilder.DropTable(
                name: "PensjeMiesieczne");

            migrationBuilder.DropTable(
                name: "Pracownicy");

            migrationBuilder.DropTable(
                name: "Uzytkownicy");
        }
    }
}
