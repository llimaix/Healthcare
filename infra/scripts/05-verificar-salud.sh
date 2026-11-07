#!/usr/bin/env bash
# Verifica la salud del stack: lista servicios, espera a que todas las replicas esten
# corriendo y comprueba el /healthz del punto de entrada nginx.
set -euo pipefail

stack="clinicacore"
# 8090 por defecto: coincide con el puerto publicado por nginx en infra/stack.yml.
puerto="${PUERTO_WEB:-8090}"
intentos="${INTENTOS:-30}"

echo "Servicios del stack ${stack}:"
docker stack services "${stack}"

echo "Esperando a que todas las replicas esten running (hasta ${intentos} intentos)..."
i=0
while [ "${i}" -lt "${intentos}" ]; do
  # Lista REPLICAS como 'actual/deseadas'; si alguna no coincide, aun no esta lista.
  pendientes="$(docker stack services "${stack}" --format '{{.Name}} {{.Replicas}}' \
    | awk '{split($2,a,"/"); if (a[1] != a[2]) print $1}')"
  if [ -z "${pendientes}" ]; then
    echo "Todas las replicas deseadas estan running."
    break
  fi
  i=$((i + 1))
  sleep 5
done

if [ -n "${pendientes:-}" ]; then
  echo "AVISO: servicios con replicas incompletas: ${pendientes}" >&2
fi

echo "Comprobando http://localhost:${puerto}/healthz ..."
if curl -fsS "http://localhost:${puerto}/healthz" >/dev/null; then
  echo "healthz OK."
else
  echo "AVISO: /healthz no respondio 200." >&2
  exit 1
fi
