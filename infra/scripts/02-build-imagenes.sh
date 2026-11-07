#!/usr/bin/env bash
# Construye las 4 imagenes de ClinicaCore con tag fijo ':local' (el que usa stack.yml).
# Idempotente: reconstruye siempre; Docker reutiliza capas cacheadas.
#
# Contextos (ver findings de FEAT-001):
#   Api, Gateway, migrator -> contexto backend/ con -f backend/Dockerfile.<x>
#   web (nginx+SPA)        -> contexto frontend/ con -f frontend/Dockerfile
set -euo pipefail

# Raiz del repo = dos niveles arriba de infra/scripts/.
raiz="$(cd "$(dirname "$0")/../.." && pwd)"
cd "${raiz}"

echo "Construyendo clinicacore-web:local ..."
docker build -f frontend/Dockerfile -t clinicacore-web:local frontend

echo "Construyendo clinicacore-gateway:local ..."
docker build -f backend/Dockerfile.gateway -t clinicacore-gateway:local backend

echo "Construyendo clinicacore-api:local ..."
docker build -f backend/Dockerfile.api -t clinicacore-api:local backend

echo "Construyendo clinicacore-migrator:local ..."
docker build -f backend/Dockerfile.migrator -t clinicacore-migrator:local backend

echo "Imagenes construidas:"
docker image ls --format '  {{.Repository}}:{{.Tag}} ({{.Size}})' | grep clinicacore || true
