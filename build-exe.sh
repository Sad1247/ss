#!/usr/bin/env bash
# Compile PlanBureaux.exe (Windows 64 bits) depuis Linux ou macOS.
# La page plan-bureaux.html est la source unique : elle est copiée dans
# desktop/ le temps de la compilation, car go:embed ne lit que le dossier
# du paquet.
set -euo pipefail
root="$(cd "$(dirname "$0")" && pwd)"

cp "$root/plan-bureaux.html" "$root/desktop/plan-bureaux.html"
trap 'rm -f "$root/desktop/plan-bureaux.html"' EXIT

cd "$root/desktop"
GOOS=windows GOARCH=amd64 CGO_ENABLED=0 \
  go build -trimpath -ldflags "-s -w" -o "$root/PlanBureaux.exe" .

echo "PlanBureaux.exe : $(du -h "$root/PlanBureaux.exe" | cut -f1)"
