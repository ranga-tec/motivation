# Contabo deployment (single VPS)

POMS runs on one Ubuntu 24.04 VPS as four Docker containers: PostgreSQL, the API, the React
frontend, and Caddy. Caddy obtains a Let's Encrypt certificate automatically and serves the
whole system from one HTTPS address. Files live in `POMS/deploy/contabo/`.

| Item | Value |
| --- | --- |
| Server | Contabo Cloud VPS 4, Hub Europe, `169.58.173.191` |
| Address | <https://169-58-173-191.sslip.io> (free sslip.io name for the server IP) |
| Ports open | 22 (SSH), 80 and 443 (Caddy). API and database are not exposed. |
| Data | Docker volumes `poms_database`, `poms_files` (uploads), `poms_keys` (login token keys) |
| Backups | Nightly 02:30 to `/var/backups/poms`, kept 14 days, plus Contabo Auto Backup |

## First install

```bash
ssh root@169.58.173.191
git clone https://github.com/ranga-tec/motivation.git /opt/poms
cd /opt/poms/POMS/deploy/contabo
./setup-server.sh          # installs Docker + firewall, creates .env, then stops
nano .env                  # set BOOTSTRAP_ADMIN_EMAIL and BOOTSTRAP_ADMIN_PASSWORD
./setup-server.sh          # builds and starts everything
```

The first administrator is created only while the database is empty. After that, manage users
in **Administration → Users & access**; the bootstrap values may then be removed from `.env`.

## Update to the latest code

```bash
cd /opt/poms && git pull
cd POMS/deploy/contabo && docker compose up -d --build
```

## Checks

```bash
docker compose ps
curl -s https://169-58-173-191.sslip.io/health      # Healthy
docker compose logs -f api                          # API log
tail /var/log/poms-backup.log                       # last backups
```

## Restore from a backup

```bash
cd /opt/poms/POMS/deploy/contabo
docker compose exec -T database pg_restore -U poms -d poms --clean --if-exists < /var/backups/poms/db_<stamp>.dump
docker run --rm -v poms_files:/files -v /var/backups/poms:/backup alpine sh -c "cd /files && tar xzf /backup/files_<stamp>.tar.gz"
docker compose restart api
```

## Using your own domain later

1. Create an `A` record for the domain pointing to `169.58.173.191`.
2. Set `POMS_DOMAIN=your.domain` in `.env`.
3. Run `docker compose up -d --build` (the frontend is rebuilt with the new address).
