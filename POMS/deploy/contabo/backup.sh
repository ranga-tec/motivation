#!/usr/bin/env bash
# Nightly POMS backup: PostgreSQL dump + uploaded files. Keeps 14 days in /var/backups/poms.
# Installed by setup-server.sh as /etc/cron.d/poms-backup.
set -euo pipefail
cd "$(dirname "$0")"
target=/var/backups/poms
stamp=$(date +%Y-%m-%d_%H%M)
mkdir -p "$target"
docker compose exec -T database pg_dump -U poms -d poms --format=custom > "$target/db_$stamp.dump"
docker run --rm -v poms_files:/files:ro -v "$target":/backup alpine tar czf "/backup/files_$stamp.tar.gz" -C /files .
find "$target" -type f -mtime +14 -delete
echo "$(date -Is) backup ok: db_$stamp.dump files_$stamp.tar.gz"
