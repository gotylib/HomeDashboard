# Home Dashboard

Personal home-server portal: service tiles with health checks, widgets, custom wallpaper, and authenticated edit mode.

## Stack

- **SPA:** React + Vite + TypeScript, served by ASP.NET Core from `wwwroot`
- **API:** ASP.NET Core 10 + EF Core
- **Database:** PostgreSQL 16
- **Deploy:** single Docker image (`app`) + Postgres

## Quick start (Docker)

```bash
cp .env.example .env
# edit ADMIN_PASSWORD and JWT_KEY
docker compose up --build -d
```

Open http://localhost:8080

Default login: `admin` / `admin` (or values from `.env`).

One container serves both the React UI and the API (`/api`, `/uploads`).

## Local development

### 1. PostgreSQL

```bash
docker compose up -d db
```

### 2. API

```bash
cd backend/Home.Api
dotnet run --launch-profile http
```

API: http://localhost:5010

### 3. Frontend (Vite proxy)

```bash
cd frontend
npm install
npm run dev
```

Vite: http://localhost:5173 (proxies `/api` and `/uploads` to the API).

## Features

- Public glass dashboard over wallpaper (image / GIF / MP4)
- Service tiles with icon, link, and green/red health indicator
- Widgets: clock, weather, notes, search, countdown
- Edit mode: add/edit/delete tiles, drag & resize, browse wallpaper/icons
- Background health checks every ~30s

## Widgets

| Type | Description |
|------|-------------|
| `clock` | Local/timezone clock |
| `weather` | Open-Meteo by city |
| `notes` | Sticky note text |
| `search` | DuckDuckGo / Google / Bing |
| `countdown` | Countdown to a date/time |

## API overview

| Method | Path | Auth |
|--------|------|------|
| GET | `/api/dashboard` | no |
| POST | `/api/auth/login` | no |
| POST | `/api/auth/logout` | yes |
| GET | `/api/auth/me` | yes |
| CRUD | `/api/services` | write: yes |
| CRUD | `/api/widgets` | write: yes |
| PUT | `/api/layout` | yes |
| PUT | `/api/settings/wallpaper` | yes |
| DELETE | `/api/settings/wallpaper` | yes |
| POST | `/api/uploads` | yes |
| DELETE | `/api/uploads?path=` | yes |

## CI/CD (Docker Hub)

Push to the `prom` branch runs [`.github/workflows/docker.yml`](.github/workflows/docker.yml):

1. Reads `VERSION` from [`version.env`](version.env)
2. Retags existing `latest` → `v$VERSION` (if present)
3. Builds SPA image and pushes `homedashboard:latest`

GitHub secrets required:

- `DOCKERHUB_USERNAME`
- `DOCKERHUB_TOKEN`

Image: `<DOCKERHUB_USERNAME>/homedashboard:latest`

## Environment / Dokploy

The app reads:

1. Process environment (Dokploy UI / Docker `environment`) — highest priority  
2. Optional `.env` file (`ENV_FILE`, `/app/.env`, or cwd) — only fills missing keys  
3. `appsettings.json` defaults

Flat names (Dokploy-friendly) and ASP.NET `__` names both work.

| Variable | Required | Description |
|----------|----------|-------------|
| `ADMIN_USERNAME` | yes* | Admin login (seeded on first start) |
| `ADMIN_PASSWORD` | yes* | Admin password |
| `JWT_KEY` | yes* | Secret for auth cookies (32+ chars) |
| `DATABASE_HOST` | yes** | Postgres host |
| `DATABASE_PORT` | no | Default `5432` |
| `DATABASE_NAME` | no | Default `home_dashboard` |
| `DATABASE_USER` | yes** | Postgres user |
| `DATABASE_PASSWORD` | yes** | Postgres password |
| `CONNECTION_STRING` | alt** | Full Npgsql string instead of DATABASE_* |
| `UPLOADS_PATH` | no | Default `/app/uploads` |
| `JWT_ISSUER` | no | Default `Home.Api` |
| `JWT_AUDIENCE` | no | Default `Home.Web` |
| `JWT_EXPIRE_HOURS` | no | Default `72` |
| `ASPNETCORE_ENVIRONMENT` | no | `Production` / `Development` |
| `ENV_FILE` | no | Path to `.env` inside container |
| `PORT` | no | Host port for compose only (`8080`) |

\* Change defaults in production.  
\*\* Either `CONNECTION_STRING` **or** `DATABASE_*` (aliases: `POSTGRES_HOST`, `POSTGRES_DB`, …).

Also accepted: `ConnectionStrings__Default`, `Admin__Username`, `Jwt__Key`, `Uploads__Path`.

See [`.env.example`](.env.example).

## Notes

- Uploaded files live in `uploads/` (Docker volume).
- Change `ADMIN_PASSWORD` and `JWT_KEY` before exposing on a network.
- Weather uses public Open-Meteo APIs from the browser (no API key).
