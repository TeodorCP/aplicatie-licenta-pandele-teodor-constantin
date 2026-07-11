#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
FRONTEND_DIR="$ROOT_DIR/frontend"
FRONTEND_HOST="${FRONTEND_HOST:-127.0.0.1}"
FRONTEND_PORT="${FRONTEND_PORT:-3000}"
BACKEND_HOST="${BACKEND_HOST:-127.0.0.1}"
BACKEND_PORT="${BACKEND_PORT:-5000}"
FRONTEND_URL="${FRONTEND_URL:-http://${FRONTEND_HOST}:${FRONTEND_PORT}}"
BACKEND_URLS="${BACKEND_URLS:-http://${BACKEND_HOST}:${BACKEND_PORT}}"
LOCAL_NODE_DIR="${LOCAL_NODE_DIR:-$ROOT_DIR/.local/node}"
LOCAL_NODE_BIN_DIR="$LOCAL_NODE_DIR/bin"

BACKEND_PID=""
FRONTEND_PID=""

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Error: '$1' is required but not installed." >&2
    exit 1
  fi
}

activate_local_node() {
  if [[ -x "$LOCAL_NODE_BIN_DIR/node" && -x "$LOCAL_NODE_BIN_DIR/npm" ]]; then
    export PATH="$LOCAL_NODE_BIN_DIR:$PATH"
  fi
}

cleanup() {
  local exit_code=$?

  if [[ -n "${BACKEND_PID}" ]] && kill -0 "${BACKEND_PID}" >/dev/null 2>&1; then
    kill "${BACKEND_PID}" >/dev/null 2>&1 || true
  fi

  if [[ -n "${FRONTEND_PID}" ]] && kill -0 "${FRONTEND_PID}" >/dev/null 2>&1; then
    kill "${FRONTEND_PID}" >/dev/null 2>&1 || true
  fi

  wait "${BACKEND_PID}" "${FRONTEND_PID}" 2>/dev/null || true
  exit "${exit_code}"
}

wait_for_url() {
  local url=$1
  local retries=${2:-60}

  for ((i = 1; i <= retries; i += 1)); do
    if curl --silent --fail "${url}" >/dev/null 2>&1; then
      return 0
    fi

    sleep 1
  done

  return 1
}

open_browser() {
  local url=$1

  if [[ "${OPEN_BROWSER:-1}" != "1" ]]; then
    echo "Browser auto-open disabled. Visit ${url}"
    return 0
  fi

  if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "${url}" >/dev/null 2>&1 &
  elif command -v open >/dev/null 2>&1; then
    open "${url}" >/dev/null 2>&1 &
  elif command -v cmd.exe >/dev/null 2>&1; then
    cmd.exe /c start "${url}" >/dev/null 2>&1 &
  else
    echo "Browser could not be opened automatically. Visit ${url}"
  fi
}

activate_local_node
require_command dotnet
require_command npm
require_command curl

if [[ ! -d "${BACKEND_DIR}" ]]; then
  echo "Error: backend directory not found at ${BACKEND_DIR}" >&2
  exit 1
fi

if [[ ! -d "${FRONTEND_DIR}" ]]; then
  echo "Error: frontend directory not found at ${FRONTEND_DIR}" >&2
  exit 1
fi

trap cleanup EXIT INT TERM

echo "Starting backend on ${BACKEND_URLS}..."
(
  cd "${BACKEND_DIR}"
  ASPNETCORE_URLS="${BACKEND_URLS}" dotnet run --launch-profile http
) &
BACKEND_PID=$!

echo "Starting frontend on ${FRONTEND_URL}..."
(
  cd "${FRONTEND_DIR}"
  npm run dev -- --host "${FRONTEND_HOST}" --port "${FRONTEND_PORT}"
) &
FRONTEND_PID=$!

if wait_for_url "${FRONTEND_URL}"; then
  echo "Opening ${FRONTEND_URL} in your browser..."
  open_browser "${FRONTEND_URL}"
else
  echo "Frontend did not become ready in time. Visit ${FRONTEND_URL} manually."
fi

echo "Backend PID: ${BACKEND_PID}"
echo "Frontend PID: ${FRONTEND_PID}"
echo "Press Ctrl+C to stop both servers."

wait "${BACKEND_PID}" "${FRONTEND_PID}"
