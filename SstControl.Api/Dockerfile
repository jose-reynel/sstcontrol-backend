FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore SstControl.Api/SstControl.Api.csproj
RUN dotnet publish SstControl.Api/SstControl.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# curl: usado por el HEALTHCHECK de abajo (y el que declara docker-compose.yml)
#   contra el endpoint /salud.
# libtesseract-dev + libleptonica-dev: binarios nativos que envuelve el paquete
#   NuGet "Tesseract" (P/Invoke) — sin ellos, ServicioOcrTesseract falla en
#   tiempo de ejecución al crear el TesseractEngine, no en la compilación.
# tesseract-ocr-spa: datos de idioma español entrenados (Ocr:Idioma=spa en
#   appsettings.json) — sin este paquete el motor arranca pero reconoce muy
#   poco o nada en documentos en español.
RUN apt-get update && apt-get install -y --no-install-recommends \
        curl libtesseract-dev libleptonica-dev tesseract-ocr-spa \
    && rm -rf /var/lib/apt/lists/*
# Ruta real donde Debian instala los datos de idioma de Tesseract — coincide
# con la clave de configuración Ocr:RutaDatosEntrenamiento (appsettings.json).
ENV Ocr__RutaDatosEntrenamiento=/usr/share/tesseract-ocr/5/tessdata
COPY --from=build /app .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/salud || exit 1
ENTRYPOINT ["dotnet", "SstControl.Api.dll"]
