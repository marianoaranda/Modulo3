# Build context: raíz del repositorio.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore primero con sólo el csproj, para que la capa se cachee entre cambios de código.
COPY src/Stock.Web/Stock.Web.csproj src/Stock.Web/
RUN dotnet restore src/Stock.Web/Stock.Web.csproj

COPY src/Stock.Web/ src/Stock.Web/
RUN dotnet publish src/Stock.Web/Stock.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Stock.Web.dll"]
