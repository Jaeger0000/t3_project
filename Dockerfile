# syntax=docker/dockerfile:1
#
# Tek imaj: API hem JSON uçlarını hem derlenmiş arayüzü sunar.
#
# Neden tek imaj: istemci tüm isteklerini göreli /api yoluna atıyor ve bu yolu
# backend'e taşıyan tek şey Vite'ın *geliştirme* vekiliydi — yani derlenmiş
# arayüz hiçbir API'ye ulaşamıyordu. Aynı kökten sunmak CORS'u, vekil
# yapılandırmasını ve çerezin SameSite gevşetmesini birden ortadan kaldırıyor.

# --- 1) Arayüzü derle -----------------------------------------------------
FROM node:22-alpine AS frontend
WORKDIR /src

# package dosyaları ayrı katmanda: kaynak değişince npm ci yeniden çalışmasın.
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

# --- 2) API'yi yayınla ----------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /src

COPY backend/ ./
RUN dotnet restore T3.Ekosistem.sln
RUN dotnet publish src/T3.Api/T3.Api.csproj -c Release -o /app/publish --no-restore

# --- 3) Çalışma zamanı ----------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=backend /app/publish ./
COPY --from=frontend /src/dist ./wwwroot

# Yüklenen belgeler ve şifre sıfırlama kutusu konteyner dışında kalmalı
# (docker-compose.prod.yml bunları bağlıyor); klasörler kök olmayan kullanıcıya
# ait olmalı, yoksa ilk yükleme izin hatasıyla düşer.
RUN mkdir -p /app/storage/documents /app/storage/outbox \
    && chown -R $APP_UID /app/storage

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    T3_Hosting__WebRoot=/app/wwwroot

EXPOSE 8080

# Kök olmayan kullanıcı: imajın kendisi bir güvenlik sınırı.
USER $APP_UID

ENTRYPOINT ["dotnet", "T3.Api.dll"]
