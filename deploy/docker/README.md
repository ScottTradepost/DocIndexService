# Docker Assets

This folder will contain docker-compose definitions and environment templates
for local infrastructure services (PostgreSQL, Tika, and Ollama).

## Quick Start

1. Copy `.env.sample` to `.env` in this folder.
2. Start dependencies:

```powershell
docker compose --env-file .env -f .\docker-compose.yml up -d
```

3. Stop dependencies:

```powershell
docker compose --env-file .env -f .\docker-compose.yml down
```
