# ==========================================
# BASE: ASP.NET runtime
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
ENV BackendUrl=http://+:8080
EXPOSE 8080

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
