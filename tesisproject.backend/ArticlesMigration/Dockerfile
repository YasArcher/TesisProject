# ==========================================
# BASE: ASP.NET runtime
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
ENV BackendUrl=http://+:8080
EXPOSE 8080

# QuestPDF/Skia requiere fuentes y librerías nativas disponibles en Linux
# para generar documentos dentro del contenedor de producción.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        fontconfig \
        fonts-dejavu-core \
        libfreetype6 \
        libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

# ==========================================
# BUILD
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["tesisproject.backend/tesisproject.backend.csproj", "tesisproject.backend/"]
COPY ["tesisproject.shared/tesisproject.shared.csproj", "tesisproject.shared/"]
RUN dotnet restore "tesisproject.backend/tesisproject.backend.csproj"

COPY . .
WORKDIR /src/tesisproject.backend
RUN dotnet build "tesisproject.backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ==========================================
# PUBLISH
# ==========================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "tesisproject.backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ==========================================
# FINAL
# ==========================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "tesisproject.backend.dll"]
