# STL E-Commerce API

STL is an e-commerce Web API built with ASP.NET Core and FastEndpoints. The
project uses Entity Framework Core with PostgreSQL for data persistence, Redis
for distributed caching, and JWT for authentication.

## How to Run

### Prerequisites

- .NET 10 SDK
- Docker Desktop
- EF Core CLI:

```powershell
dotnet tool install --global dotnet-ef
```

### 1. Start PostgreSQL and Redis

Run from the repository root:

```powershell
docker compose up -d
docker compose ps
```

### 2. Configure the Application

Set the local connection strings and JWT signing key:

### 3. Apply Database Migrations

```powershell
dotnet ef database update --project .\Domain\STL.Core.csproj --startup-project .\STL\STL.WebApis.csproj
```

### 4. Run the API

```powershell
dotnet run --project .\STL\STL.WebApis.csproj
```

Open the Swagger URL shown in the application console to access the API
documentation.

### Stop Local Services

```powershell
docker compose down
```
