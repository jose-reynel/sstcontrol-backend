FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore SstControl.Api/SstControl.Api.csproj
RUN dotnet publish SstControl.Api/SstControl.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# curl se usa únicamente para el HEALTHCHECK de abajo (y el que declara
# docker-compose.yml) contra el endpoint /salud.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/salud || exit 1
ENTRYPOINT ["dotnet", "SstControl.Api.dll"]
