# Contabo Deployment

This document describes the prepared same-VPS fallback path. The current live deployment is on Railway, not Contabo.

This project can be deployed on the Contabo server without touching the existing ISS app on port `80`.

## Recommended shape

- Keep ISS on port `80`
- Run POMS in Docker on port `8081`
- Keep PostgreSQL private inside Docker
- Persist uploaded files and ASP.NET data-protection keys with Docker volumes

## One-time setup on the server

```bash
apt-get update
apt-get install -y docker.io docker-compose-plugin
systemctl enable --now docker
```

## Deploy

```bash
git clone https://github.com/ranga-tec/motivation.git
cd motivation
cp .env.contabo.example .env
```

Update `.env` before starting:

```dotenv
POSTGRES_PASSWORD=use-a-strong-password-here
POMS_HOST_PORT=8081
SECURITY_FORCE_HTTPS=false
```

Start the stack:

```bash
docker compose -f docker-compose.contabo.yml up -d --build
```

## Smoke checks

```bash
docker compose -f docker-compose.contabo.yml ps
curl http://127.0.0.1:8081/health
curl -I http://127.0.0.1:8081/
```

Public URL on the server IP:

```text
http://178.238.230.31:8081
```

## Fresh database login accounts

These accounts are created automatically on first startup for a fresh database:

- `admin@poms.lk` / `Admin@123`
- `clinician@poms.lk` / `Clinic@123`
- `registrar@poms.lk` / `Data@123`
- `viewer@poms.lk` / `View@123`

## Notes

- The live Railway environments no longer use the seeded passwords above.
- `Security__ForceHttps=false` is intentional for an IP-and-port deployment. Turn it on only after placing the app behind HTTPS.
- If you later want a cleaner URL, put Nginx in front of `http://127.0.0.1:8081`.
- Uploaded files persist in the `poms-storage` Docker volume.
- Login cookies persist across restarts in the `poms-data-protection` Docker volume.
