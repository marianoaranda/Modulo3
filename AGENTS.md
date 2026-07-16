# AGENTS.md

## Propósito
Sitio web para un comercio de barrio que, a partir de las compras y ventas registradas,
infiere automáticamente qué artículos hace falta pedir (por stock mínimo, punto de pedido o stock ideal).

## Stack
- Front-End: ASP.NET MVC, .NET 8 (`Stock.Web`)
- Back-End: Web API REST, .NET 8, autenticación JWT (`Stock.Api`)
- Base de datos: SQL Server 2017, acceso vía EF Core Migrations
- Tests: NUnit (`dotnet test`)
- Solución: `StockModulo.sln`

## Cómo correr
```
# Instalar dependencias
dotnet restore StockModulo.sln

# Levantar todo (SQL Server + API + Web) con Docker
docker-compose up

# Aplicar migraciones de base de datos
dotnet ef database update --project src/Stock.Api

# Correr Front y Back en local sin Docker
dotnet run --project src/Stock.Api
dotnet run --project src/Stock.Web

# Correr tests
dotnet test StockModulo.sln
```

## Qué NO hacer
- No guardar ni loguear la contraseña en texto plano ni en un hash reversible: siempre hash + salt aleatorio por usuario (RF-03, RF-04).
- No implementar manejo de múltiples proveedores por artículo: está explícitamente fuera de alcance del PRD.
- No armar consultas de stock/pedido sin límite ni filtro: usar TOP 10000 y filtro opcional por descripción (`LIKE '%%'`) para mitigar el riesgo de volumen de artículos.
