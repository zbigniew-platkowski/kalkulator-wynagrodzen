# -------------------------------------------------------
# ETAP 1: Build
# Używamy obrazu SDK do kompilacji aplikacji
# -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy TYLKO plik projektu i przywracamy zależności
# (dzięki temu Docker cache'uje ten krok - nie pobiera NuGet przy każdej zmianie kodu)
COPY ["Kalkulator.API/Kalkulator.API.csproj", "Kalkulator.API/"]
RUN dotnet restore "Kalkulator.API/Kalkulator.API.csproj"

# Teraz kopiujemy cały kod i budujemy
COPY . .
WORKDIR "/src/Kalkulator.API"
RUN dotnet build "Kalkulator.API.csproj" -c Release -o /app/build

# -------------------------------------------------------
# ETAP 2: Publish
# -------------------------------------------------------
FROM build AS publish
RUN dotnet publish "Kalkulator.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -------------------------------------------------------
# ETAP 3: Runtime
# Używamy lekkiego obrazu runtime (bez SDK) - mniejszy obraz końcowy
# -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .

# Uruchomienie aplikacji
ENTRYPOINT ["dotnet", "Kalkulator.API.dll"]
