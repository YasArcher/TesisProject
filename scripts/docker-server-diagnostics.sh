#!/usr/bin/env bash
set -euo pipefail

echo "== Sistema =="
hostname || true
whoami || true
pwd || true
uname -a || true

echo
echo "== Docker =="
docker --version || true
docker compose version || true
docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Ports}}\t{{.Status}}" || true

echo
echo "== Puertos escuchando =="
if command -v ss >/dev/null 2>&1; then
  ss -tulpen || true
elif command -v netstat >/dev/null 2>&1; then
  netstat -tulpen || true
else
  echo "No se encontro ss ni netstat."
fi

echo
echo "== SQL Server local =="
docker ps --format "{{.Names}}" | grep -Ei "sql|mssql" || true
if command -v systemctl >/dev/null 2>&1; then
  systemctl status mssql-server --no-pager || true
fi

echo
echo "== Directorios sugeridos =="
ls -la /home/dinnova || true
ls -la /home/dinnova/apps 2>/dev/null || true
