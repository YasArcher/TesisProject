# ===== Build Blazor WASM =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["tesisproject.frontend/tesisproject.frontend.csproj", "tesisproject.frontend/"]
COPY ["tesisproject.shared/tesisproject.shared.csproj", "tesisproject.shared/"]
RUN dotnet restore "tesisproject.frontend/tesisproject.frontend.csproj"

COPY . .
WORKDIR /src/tesisproject.frontend
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/dist

# ===== Runtime Nginx =====
FROM nginx:alpine AS final

# Blazor estáticos
COPY --from=build /app/dist/wwwroot /usr/share/nginx/html

# Nginx configs (main + server)
COPY tesisproject.frontend/nginx/nginx.conf /etc/nginx/nginx.conf
COPY tesisproject.frontend/nginx/default.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
