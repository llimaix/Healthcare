#!/usr/bin/env bash
# Inicializa Docker Swarm si aun no esta activo. Idempotente: si ya hay un Swarm
# activo no hace nada.
set -euo pipefail

estado="$(docker info --format '{{.Swarm.LocalNodeState}}' 2>/dev/null || echo inactive)"

if [ "${estado}" = "active" ]; then
  echo "Swarm ya activo; nada que hacer."
  exit 0
fi

echo "Inicializando Docker Swarm..."
docker swarm init >/dev/null
echo "Swarm inicializado."
