#!/usr/bin/env bash
# One-time setup on a fresh Ubuntu 24.04 VPS. Run as root from POMS/deploy/contabo after cloning.
set -euo pipefail
cd "$(dirname "$0")"

apt-get update
apt-get install -y ca-certificates curl ufw
if ! command -v docker >/dev/null; then curl -fsSL https://get.docker.com | sh; fi

ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443
ufw --force enable

if [ ! -f .env ]; then
  cp .env.example .env
  sed -i "s|^POSTGRES_PASSWORD=.*|POSTGRES_PASSWORD=$(openssl rand -base64 32 | tr -d '/+=')|" .env
  echo "Created .env with a generated database password. Set BOOTSTRAP_ADMIN_EMAIL and BOOTSTRAP_ADMIN_PASSWORD, then re-run."
  exit 1
fi

chmod 600 .env
chmod +x backup.sh
echo "30 2 * * * root $(pwd)/backup.sh >> /var/log/poms-backup.log 2>&1" > /etc/cron.d/poms-backup
docker compose up -d --build
docker compose ps
