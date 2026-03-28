# DocIndexService

## Setup

### Prerequisites

- .NET SDK 8.x
- Docker Desktop
- `dotnet-ef` CLI available (`dotnet ef --version`)

### 1) Start local dependencies

From repo root:

```powershell
Copy-Item .\deploy\docker\.env.sample .\deploy\docker\.env -Force
docker compose --env-file .\deploy\docker\.env -f .\deploy\docker\docker-compose.yml up -d
```

If you are already inside `deploy\`, use:

```powershell
docker compose --env-file .\docker\.env -f .\docker\docker-compose.yml up -d
```

This starts:

- PostgreSQL on `localhost:5432`
- Apache Tika on `localhost:9998`
- Ollama on `localhost:11434`

To stop:

```powershell
docker compose --env-file .\deploy\docker\.env -f .\deploy\docker\docker-compose.yml down
```

Port note:

- If `5432` is already in use on your machine, change `POSTGRES_PORT` in `deploy\docker\.env` to an open port (for example `5433`) and set `Postgres__ConnectionString`/`DOCINDEX_POSTGRES_CONNECTIONSTRING` to match.

### 2) Apply database migrations

From repo root:

```powershell
dotnet ef database update --project .\src\DocIndexService.Infrastructure\DocIndexService.Infrastructure.csproj --startup-project .\src\DocIndexService.Api\DocIndexService.Api.csproj
```

Optional: use a custom connection string for design-time operations:

```powershell
$env:DOCINDEX_POSTGRES_CONNECTIONSTRING = "Host=localhost;Port=5432;Database=docindexservice;Username=postgres;Password=postgres"
```

### 3) Run the applications

Run API:

```powershell
dotnet run --project .\src\DocIndexService.Api\DocIndexService.Api.csproj
```

Run Admin UI:

```powershell
dotnet run --project .\src\DocIndexService.Admin\DocIndexService.Admin.csproj
```

Run Worker:

```powershell
dotnet run --project .\src\DocIndexService.Worker\DocIndexService.Worker.csproj
```

### 4) Open the UI and API

- Admin UI: `http://localhost:5170`
- API Swagger: `http://localhost:5166/swagger`

Default development admin login:

- Username: `admin`
- Password: `Admin#12345`

Once API + Admin are running and migrations are applied, you can log in immediately and start using the UI.

### Migration workflow (new migration)

```powershell
dotnet ef migrations add <MigrationName> --project .\src\DocIndexService.Infrastructure\DocIndexService.Infrastructure.csproj --startup-project .\src\DocIndexService.Api\DocIndexService.Api.csproj
dotnet ef database update --project .\src\DocIndexService.Infrastructure\DocIndexService.Infrastructure.csproj --startup-project .\src\DocIndexService.Api\DocIndexService.Api.csproj
```

## Architecture

DocIndexService is scaffolded as a modular monolith with separate projects for API, Admin, Worker, Core, Application, Infrastructure, Contracts, and Tests.

TODO: Add module boundaries, dependency rules, and runtime interaction diagrams.
