# STAGE 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем только файлы проектов для максимального использования Docker-кэша nuget-пакетов
COPY ["UrlShortener/UrlShortener.csproj", "UrlShortener/"]
COPY ["UrlShortener.Tests/UrlShortener.Tests.csproj", "UrlShortener.Tests/"]
RUN dotnet restore "UrlShortener/UrlShortener.csproj"

# Копируем исходный код бэкенда
COPY UrlShortener/ UrlShortener/
WORKDIR "/src/UrlShortener"
RUN dotnet publish "UrlShortener.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 2: Chiseled Production Runtime
# 9.0-noble-chiseled — официальный дистролесс-образ .NET 9 на базе Ubuntu 24.04 LTS Noble.
# В нем отсутствуют shell и пакетные менеджеры для полной защиты от RCE-атак.
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS final
WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID

EXPOSE 5200
ENV ASPNETCORE_URLS=http://+:5200

ENTRYPOINT ["dotnet", "UrlShortener.dll"]