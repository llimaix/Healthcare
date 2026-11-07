#!/usr/bin/env bash
# Despliega (o actualiza) el stack ClinicaCore en Swarm. Idempotente:
# 'docker stack deploy' crea o reconcilia los servicios segun stack.yml.
set -euo pipefail

infra="$(cd "$(dirname "$0")/.." && pwd)"

echo "Desplegando stack 'clinicacore' desde ${infra}/stack.yml ..."
docker stack deploy -c "${infra}/stack.yml" clinicacore

echo "Servicios del stack:"
docker stack services clinicacore
