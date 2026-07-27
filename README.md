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

## Notes

- Uploaded files live in `uploads/` (Docker volume).
- Change `ADMIN_PASSWORD` and `Jwt__Key` before exposing on a network.
- Weather uses public Open-Meteo APIs from the browser (no API key).
