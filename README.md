# STL

ASP.NET Core API using FastEndpoints, EF Core SQLite, AutoMapper, Sieve, and
Redis distributed cache.

## Redis

Start the single Redis container from the repository root:

```powershell
docker compose up -d
docker compose exec redis redis-cli ping
```

The expected response is `PONG`.

Run the API:

```powershell
dotnet run --project .\STL\STL.csproj
```

The default Redis connection is `localhost:6379`. Override it with:

```powershell
$env:ConnectionStrings__Redis = "redis-host:6379,abortConnect=false"
```

Cache entries expire after 10 minutes by default. Override the TTL with the
`Cache__AbsoluteExpirationMinutes` environment variable.

## Cache behavior

- `GET /api/category/{id}` caches a category by ID.
- `GET /api/product/{id}` caches a product by ID.
- Update and delete endpoints invalidate the corresponding key after the
  database transaction commits.
- Redis failures are logged and reads fall back to SQLite.
