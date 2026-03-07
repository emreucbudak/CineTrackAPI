# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY CineTrackAPI.slnx .
COPY src/CineTrack.Domain/CineTrack.Domain.csproj src/CineTrack.Domain/
COPY src/CineTrack.Application/CineTrack.Application.csproj src/CineTrack.Application/
COPY src/CineTrack.Persistence/CineTrack.Persistence.csproj src/CineTrack.Persistence/
COPY src/CineTrack.Infrastructure/CineTrack.Infrastructure.csproj src/CineTrack.Infrastructure/
COPY src/CineTrack.API/CineTrack.API.csproj src/CineTrack.API/

RUN dotnet restore CineTrackAPI.slnx

# Copy everything else and build
COPY . .
RUN dotnet publish src/CineTrack.API/CineTrack.API.csproj -c Release -o /app/publish --no-restore

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

RUN chown -R appuser:appgroup /app
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CineTrack.API.dll"]
