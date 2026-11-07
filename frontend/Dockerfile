# Imagen de produccion del frontend ClinicaCore: SPA compilado servido por nginx.
# Contexto de build: frontend/ (consistente con los scripts de FEAT-002).
#   docker build -f frontend/Dockerfile -t clinicacore-web:local frontend
#
# La config de nginx se copia desde frontend/nginx/default.conf (copia del canonico
# infra/nginx/default.conf): asi el contexto frontend/ se basta por si solo.

# --- Etapa build: Node 22 (el dev usa Node 26, solo aviso EBADENGINE) ---
FROM node:22 AS build
WORKDIR /app

# VITE_GATEWAY_URL se hornea en build: mismo origen (/api), sin CORS.
ARG VITE_GATEWAY_URL=/api
ENV VITE_GATEWAY_URL=$VITE_GATEWAY_URL

# Instalacion con versiones exactas del lockfile.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# --- Etapa runtime: nginx alpine sirviendo el SPA ---
FROM nginx:1.27-alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx/default.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
