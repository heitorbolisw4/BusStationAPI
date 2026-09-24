# syntax=docker/dockerfile:1

# ---- build: SDK completo, só para compilar e publicar ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# restore em camada própria: só refaz quando o .csproj muda
COPY BusStation_API.csproj ./
RUN dotnet restore BusStation_API.csproj

COPY . .
RUN dotnet publish BusStation_API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# efbundle: executável com as migrations, rodado pelo Pre-Deploy Command do Railway
# (./efbundle) antes de cada deploy. Versão do dotnet-ef fixada em dotnet-tools.json.
RUN dotnet tool restore \
 && dotnet ef migrations bundle --project BusStation_API.csproj --configuration Release \
      --target-runtime linux-x64 --output /app/publish/efbundle --force \
 && chmod +x /app/publish/efbundle

# ---- runtime: só o ASP.NET Core runtime, sem SDK ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Porta: o Render injeta PORT (a API usa quando existe); sem ela vale esta.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# usuário não-root que já vem na imagem aspnet (uid 1654)
USER $APP_UID

ENTRYPOINT ["dotnet", "BusStation_API.dll"]
