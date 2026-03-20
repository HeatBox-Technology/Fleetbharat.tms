# ----------------------------
# Runtime Image
# ----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Expose TMS port
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production

# ----------------------------
# Build Image
# ----------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file first (better layer caching)
COPY FleetBharat.TMSService.csproj ./
RUN dotnet restore

# Copy remaining source code
COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ----------------------------
# Final Image
# ----------------------------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FleetBharat.TMSService.dll"]