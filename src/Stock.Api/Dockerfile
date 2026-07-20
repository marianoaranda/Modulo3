# Build context: raíz del repositorio.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore primero con sólo el csproj, para que la capa se cachee entre cambios de código.
COPY src/Stock.Api/Stock.Api.csproj src/Stock.Api/
RUN dotnet restore src/Stock.Api/Stock.Api.csproj

COPY src/Stock.Api/ src/Stock.Api/
RUN dotnet publish src/Stock.Api/Stock.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Stock.Api.dll"]
