#!/usr/bin/env bash
# Generuje wynikowy Excel z PDF-ów KPiR (macOS / Linux).
# Uzycie:  ./run.sh [folder_pdf] [szablon_xlsx] [folder_wyjsciowy]
# Domyslnie: ./KPIR  ./templates/Ewidencja_IP_BOX-wzor.xlsx  ./out
set -e
cd "$(dirname "$0")"

KPIR="${1:-./KPIR}"
TEMPLATE="${2:-./templates/Ewidencja_IP_BOX-wzor.xlsx}"
OUT="${3:-./out}"

# Sprawdzenie zaleznosci
for dep in dotnet pdftoppm python3; do
  if ! command -v "$dep" >/dev/null 2>&1; then
    echo "BRAK: '$dep' nie jest zainstalowane."
    echo "  dotnet:   brew install dotnet@8"
    echo "  pdftoppm: brew install poppler"
    echo "  python3:  wbudowany w macOS (doinstaluj: pip3 install opencv-python-headless)"
    exit 1
  fi
done
if ! python3 -c "import cv2" >/dev/null 2>&1; then
  echo "BRAK modulu Python 'cv2'. Zainstaluj: pip3 install opencv-python-headless"
  exit 1
fi

mkdir -p "$KPIR" "$OUT"
if ! ls "$KPIR"/*.pdf >/dev/null 2>&1; then
  echo "Folder '$KPIR' nie zawiera zadnych PDF-ow. Wrzuc tam pliki KPiR i uruchom ponownie."
  exit 1
fi

echo "Generuje ewidencje z: $KPIR"
dotnet run --project IpBoxKpirImporter.csproj -c Release -- "$KPIR" "$TEMPLATE" "$OUT"
echo ""
echo "Gotowe. Wynik: $OUT/Ewidencja_IP_BOX_z_importem.xlsx"
